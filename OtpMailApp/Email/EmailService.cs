using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using OtpMailApp.Shared;

namespace OtpMailApp.Email;

public record EmailMessageDto(
    UniqueId Uid,
    string MessageId,
    string Subject,
    InternetAddressList From,
    InternetAddressList To,
    InternetAddressList Cc,
    InternetAddressList Bcc,
    DateTimeOffset? Date,
    IDictionary<string, string> Headers,
    string TextBody,
    string HtmlBody
);

public class EmailService : IDisposable
{
    private readonly EmailServiceConfig _cfg;
    private bool _disposed;

    public EmailService(EmailServiceConfig cfg)
    {
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
    }
    public async Task SendAsync(
        string fromName,
        string? fromAddress,
        IEnumerable<string> toAddresses,
        string subject,
        string textBody = null,
        string htmlBody = null,
        IDictionary<string, string> headers = null,
        CancellationToken cancellationToken = default)
    {
        fromAddress ??= _cfg.SmtpUser;
        fromName ??= string.Empty;

        if (string.IsNullOrWhiteSpace(fromAddress)) throw new ArgumentException("fromAddress required");
        if (toAddresses == null || !toAddresses.Any()) throw new ArgumentException("At least one recipient required");

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(fromName, fromAddress));

        foreach (var to in toAddresses)
            message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject ?? string.Empty;

        var bodyBuilder = new BodyBuilder();

        if (!string.IsNullOrEmpty(textBody)) bodyBuilder.TextBody = textBody;
        if (!string.IsNullOrEmpty(htmlBody)) bodyBuilder.HtmlBody = htmlBody;

        message.Body = bodyBuilder.ToMessageBody();

        if (headers != null)
        {
            foreach (var kv in headers)
            {
                if (message.Headers.Contains(kv.Key))
                    message.Headers.Remove(kv.Key);

                message.Headers.Add(kv.Key, kv.Value ?? string.Empty);
            }
        }

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, _cfg.SmtpSecureSocketOptions, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_cfg.SmtpUser))
                await smtp.AuthenticateAsync(_cfg.SmtpUser, _cfg.SmtpPass, cancellationToken).ConfigureAwait(false);

            await smtp.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (smtp.IsConnected)
                await smtp.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        }
    }


    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_cfg.ImapHost, _cfg.ImapPort, _cfg.ImapSecureSocketOptions, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_cfg.ImapUser))
            await client.AuthenticateAsync(_cfg.ImapUser, _cfg.ImapPass, cancellationToken).ConfigureAwait(false);

        var inbox = client.GetFolder(_cfg.InboxFolderName);
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken).ConfigureAwait(false);

        int unread = 0;
        try
        {
            unread = inbox.Unread;
        }
        catch
        {
            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken).ConfigureAwait(false);
            unread = uids.Count;
        }

        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        return unread;
    }

    public async Task<IList<EmailMessageDto>> ReadUnreadAsync(bool markAsRead = true, CancellationToken cancellationToken = default)
    {
        var results = new List<EmailMessageDto>();

        using var client = new ImapClient();
        await client.ConnectAsync(_cfg.ImapHost, _cfg.ImapPort, _cfg.ImapSecureSocketOptions, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(_cfg.ImapUser))
            await client.AuthenticateAsync(_cfg.ImapUser, _cfg.ImapPass, cancellationToken).ConfigureAwait(false);

        var inbox = client.GetFolder(_cfg.InboxFolderName);
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);

        var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken).ConfigureAwait(false);
        if (uids == null || uids.Count == 0)
        {
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
            return results;
        }

        var items = await inbox.FetchAsync(uids, MessageSummaryItems.Full | MessageSummaryItems.UniqueId | MessageSummaryItems.BodyStructure, cancellationToken).ConfigureAwait(false);

        foreach (var summary in items.OrderByDescending(i => i.Date).Take(3))
        {
            var msg = await inbox.GetMessageAsync(summary.UniqueId, cancellationToken).ConfigureAwait(false);
            if (msg.Headers.FirstOrDefault(h => h.Field == EmailConstant.OtpHeader) == null)
                continue; // skip messages without OTP header

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in msg.Headers)
                headers[h.Field] = h.Value;

            string textBody = msg.TextBody ?? GetTextFromBodyParts(msg.Body);
            string htmlBody = msg.HtmlBody ?? GetHtmlFromBodyParts(msg.Body);
         
            var dto = new EmailMessageDto(
                summary.UniqueId,
                msg.MessageId ?? string.Empty,
                msg.Subject ?? string.Empty,
                msg.From,
                msg.To,
                msg.Cc,
                msg.Bcc,
                msg.Date,
                headers,
                textBody ?? string.Empty,
                htmlBody ?? string.Empty
            );

            results.Add(dto);

            if (markAsRead)
                await inbox.AddFlagsAsync(summary.UniqueId, MessageFlags.Seen, true, cancellationToken).ConfigureAwait(false);
        }

        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
        return results;
    }


    public async Task MarkAsReadAsync(UniqueId uid, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_cfg.ImapHost, _cfg.ImapPort, _cfg.ImapSecureSocketOptions, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(_cfg.ImapUser))
            await client.AuthenticateAsync(_cfg.ImapUser, _cfg.ImapPass, cancellationToken).ConfigureAwait(false);

        var inbox = client.GetFolder(_cfg.InboxFolderName);
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
        await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(UniqueId uid, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_cfg.ImapHost, _cfg.ImapPort, _cfg.ImapSecureSocketOptions, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(_cfg.ImapUser))
            await client.AuthenticateAsync(_cfg.ImapUser, _cfg.ImapPass, cancellationToken).ConfigureAwait(false);

        var inbox = client.GetFolder(_cfg.InboxFolderName);
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
        await inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true, cancellationToken).ConfigureAwait(false);
        await inbox.ExpungeAsync(cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(UniqueId uid, string destinationFolderName, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_cfg.ImapHost, _cfg.ImapPort, _cfg.ImapSecureSocketOptions, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(_cfg.ImapUser))
            await client.AuthenticateAsync(_cfg.ImapUser, _cfg.ImapPass, cancellationToken).ConfigureAwait(false);

        var inbox = client.GetFolder(_cfg.InboxFolderName);
        var dest = client.GetFolder(destinationFolderName);
        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
        await dest.OpenAsync(FolderAccess.ReadWrite, cancellationToken).ConfigureAwait(false);

        await inbox.MoveToAsync(uid, dest, cancellationToken).ConfigureAwait(false);
        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);
    }


    private static string GetTextFromBodyParts(MimeEntity body)
    {
        // traverse to find first text/plain
        if (body == null) return null;
        if (body is TextPart tp && tp.IsPlain) return tp.Text;
        if (body is Multipart mp)
        {
            foreach (var part in mp)
            {
                var t = GetTextFromBodyParts(part);
                if (!string.IsNullOrEmpty(t)) return t;
            }
        }
        return null;
    }

    private static string GetHtmlFromBodyParts(MimeEntity body)
    {
        if (body == null) return null;
        if (body is TextPart tp && tp.IsHtml) return tp.Text;
        if (body is Multipart mp)
        {
            foreach (var part in mp)
            {
                var t = GetHtmlFromBodyParts(part);
                if (!string.IsNullOrEmpty(t)) return t;
            }
        }
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        // nothing to dispose currently, but implement if later holds stateful clients
        _disposed = true;
    }
}
