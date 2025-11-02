using OtpMailApp.Cipher;
using OtpMailApp.Email;
using OtpMailApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands;

public class SendEmailCommand : ICommand
{
    private readonly EmailService _emailService;
    private readonly ICipherService _otp;

    public SendEmailCommand(EmailService emailService, ICipherService otp)
    {
        _emailService = emailService;
        _otp = otp;
    }

    public void Execute()
    {
        ExecuteAsync().GetAwaiter().GetResult();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.Write("Recipient email: ");
        var to = Console.ReadLine()?.Trim();
        Console.Write("Message: ");
        var message = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(message))
        {
            Console.WriteLine("Missing input.");
            return;
        }

        byte[] key;
        string filename = "";
        Console.Write("Enter key file name (or leave empty): ");
        var enteredKeyPath = Console.ReadLine()?.Trim();
        var (resolvedPath, found) = KeyManager.ResolveKeyFilePath(enteredKeyPath ?? "");
        if (found)
        {
            key = System.IO.File.ReadAllBytes(resolvedPath);
            filename = Path.GetFileName(resolvedPath);
        }
        else
        {
            key = _otp.GenerateRandomKey(message.Length);
            filename = KeyManager.GetKeyFileName();
            var path = KeyManager.GetKeyFilePath(filename);
            KeyManager.EnsureKeysDirectoryExists();
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, key);
                Console.WriteLine($"Key saved to {path}");
            }
            else
            {
                Console.WriteLine("Key generated but not saved.");
            }
        }
        
        var encrypted = _otp.Encrypt(message, key);
        var keyBase64 = Convert.ToBase64String(key);

        await _emailService.SendAsync(
            fromName: "OTP Mailer",
            fromAddress: null,
            toAddresses: new[] { to },
            subject: "Encrypted message",
            textBody: encrypted,
            headers: new Dictionary<string, string> { { EmailConstant.OtpHeader, filename } },
            cancellationToken: cancellationToken
        );

        Console.WriteLine("Encrypted message sent successfully!");
    }
}
