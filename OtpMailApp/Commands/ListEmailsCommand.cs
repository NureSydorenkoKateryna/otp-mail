using OtpMailApp.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands;

public class ListEmailsCommand : ICommand
{
    private readonly EmailService _emailService;

    public ListEmailsCommand(EmailService emailService)
    {
        _emailService = emailService;
    }

    public void Execute()
    {
        ExecuteAsync().GetAwaiter().GetResult();
    }
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var unread = await _emailService.ReadUnreadAsync(markAsRead: false, cancellationToken);
        if (!unread.Any())
        {
            Console.WriteLine("No unread messages.");
            return;
        }

        Console.WriteLine("Unread messages:");
        foreach (var msg in unread)
        {
            Console.WriteLine($"[{msg.Uid.Id}] {msg.Subject} (from {msg.From})");
        }
    }
}
