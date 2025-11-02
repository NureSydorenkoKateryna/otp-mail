using OtpMailApp.Cipher;
using OtpMailApp.Commands;
using OtpMailApp.Email;
using OtpMailApp.Shared;

var otp = new OtpCipherService();
var emailService = (EmailService)null;
try
{
    // check for existing user email config
    var startApp = false;
    if (!ConfigsManager.ConfigFileExists())
    {
        var configCommand = new ConfigureEmailCredsCommand();
        configCommand.Execute();
    }

    if (!ConfigsManager.ConfigFileExists())
    {
        Console.WriteLine("Email configuration is required to proceed. Exiting.");
        return;
    }

    var userConfig = ConfigsManager.ReadConfigFile();
    emailService = new EmailService(userConfig.MapToServiceConfig());
    startApp = true;

    while (startApp)
    {
        Console.WriteLine("\nSelect a command:");
        Console.WriteLine("1 - Encrypt Message");
        Console.WriteLine("2 - Decrypt Message");
        Console.WriteLine("3 - Generate Key");
        Console.WriteLine("4 - List emails");
        Console.WriteLine("5 - Send Email");
        Console.WriteLine("6 - Read Email");
        Console.WriteLine("q - Exit");

        Console.Write("Enter choice: ");
        string choice = Console.ReadLine() ?? "";

        ICommand command = choice switch
        {
            "1" => new EncryptCommand(otp),
            "2" => new DecryptCommand(otp),
            "3" => new GenerateKeyCommand(otp),
            "4" => new ListEmailsCommand(emailService),
            "5" => new SendEmailCommand(emailService, otp),
            "6" => new ReadEmailCommand(emailService, otp),
            "q" => new ExitCommand(),
            _ => null
        };

        if (command != null)
            command.Execute();
        else
            Console.WriteLine("Invalid choice, try again.");
    }
}
finally
{
    emailService?.Dispose();
    Console.WriteLine("Application exiting.");
}
