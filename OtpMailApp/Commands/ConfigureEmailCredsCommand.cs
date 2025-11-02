using OtpMailApp.Shared;

namespace OtpMailApp.Commands;

public class ConfigureEmailCredsCommand : ICommand
{
    public void Execute()
    {
        Console.Write("Enter SMTP server: ");
        var smtpServer = Console.ReadLine()?.Trim();
        Console.Write("Enter SMTP port: ");
        var smtpPortInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(smtpPortInput, out int smtpPort))
        {
            Console.WriteLine("Invalid SMTP port.");
            return;
        }
        Console.Write("Enter IMAP server: ");
        var imapServer = Console.ReadLine()?.Trim();
        Console.Write("Enter IMAP port: ");
        var imapPortInput = Console.ReadLine()?.Trim();
        if (!int.TryParse(imapPortInput, out int imapPort))
        {
            Console.WriteLine("Invalid IMAP port.");
            return;
        }
        Console.Write("Enter email address: ");
        var emailAddress = Console.ReadLine()?.Trim();
        Console.Write("Enter password: ");
        var password = Console.ReadLine()?.Trim();
        var config = new UserEmailConfig
        {
            SmtpServer = smtpServer,
            SmtpPort = smtpPort,
            ImapServer = imapServer,
            ImapPort = imapPort,
            EmailAddress = emailAddress,
            Password = password
        };
        ConfigsManager.WriteConfigFile(config);
        Console.WriteLine("Email configuration saved successfully.");
    }
}
