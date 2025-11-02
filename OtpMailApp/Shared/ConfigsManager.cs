using MailKit.Security;
using OtpMailApp.Email;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace OtpMailApp.Shared;

public class EmailServiceConfig
{
    public string SmtpHost { get; init; } = "smtp.gmail.com";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUser { get; init; } = "test@gmail.com";
    public string SmtpPass { get; init; } = "hello";
    public SecureSocketOptions SmtpSecureSocketOptions { get; init; } = SecureSocketOptions.StartTls;

    // IMAP settings for reading
    public string ImapHost { get; init; } = "imap.gmail.com";
    public int ImapPort { get; init; } = 993;
    public string ImapUser { get; init; } = "tets@gmail.com";
    public string ImapPass { get; init; } = "hello";
    public SecureSocketOptions ImapSecureSocketOptions { get; init; } = SecureSocketOptions.SslOnConnect;

    // Default mailbox/folder to check for unread
    public string InboxFolderName { get; init; } = "INBOX";

}

public class UserEmailConfig
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string ImapServer { get; set; }
    public int ImapPort { get; set; }
    public string EmailAddress { get; set; }
    public string Password { get; set; }
}
public static class ConfigsManager
{

    private const string ConfigFilePath = "email_config.json";
    private const string FolderPath = "configs";

    public static string GetConfigFileFullPath()
        => System.IO.Path.Combine(FolderPath, ConfigFilePath);
    public static void EnsureConfigsDirectoryExists()
    {
        if (!System.IO.Directory.Exists(FolderPath))
        {
            System.IO.Directory.CreateDirectory(FolderPath);
        }
    }
    public static bool ConfigFileExists()
        => System.IO.File.Exists(GetConfigFileFullPath());

    public static UserEmailConfig ReadConfigFile()
    {
        EnsureConfigsDirectoryExists();
        var path = GetConfigFileFullPath();
        var json = System.IO.File.ReadAllText(path);
        var config = System.Text.Json.JsonSerializer.Deserialize<UserEmailConfig>(json);
        if (config == null)
        {
            throw new Exception("Failed to deserialize email configuration.");
        }
        return config;
    }

    public static EmailServiceConfig MapToServiceConfig(this UserEmailConfig emailConfig)
    {
        return new EmailServiceConfig
        {
            SmtpHost = emailConfig.SmtpServer,
            SmtpPort = emailConfig.SmtpPort,
            SmtpUser = emailConfig.EmailAddress,
            SmtpPass = emailConfig.Password,
            ImapHost = emailConfig.ImapServer,
            ImapPort = emailConfig.ImapPort,
            ImapUser = emailConfig.EmailAddress,
            ImapPass = emailConfig.Password
        };
    }

    public static void WriteConfigFile(UserEmailConfig config)
    {
        var path = GetConfigFileFullPath();
        EnsureConfigsDirectoryExists();
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
    }
}
