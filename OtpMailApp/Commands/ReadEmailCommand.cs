using OtpMailApp.Cipher;
using OtpMailApp.Email;
using OtpMailApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands;

public class ReadEmailCommand : ICommand
{
    private readonly EmailService _emailService;
    private readonly ICipherService _otp;

    public ReadEmailCommand(EmailService emailService, ICipherService otp)
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
        Console.Write("Enter message UID to read: ");
        if (!uint.TryParse(Console.ReadLine(), out uint id))
        {
            Console.WriteLine("Invalid ID.");
            return;
        }

        var messages = await _emailService.ReadUnreadAsync(markAsRead: false, cancellationToken);
        var msg = messages.FirstOrDefault(m => m.Uid.Id == id);
        if (msg == null)
        {
            Console.WriteLine("Message not found.");
            return;
        }

        if (!msg.Headers.TryGetValue(EmailConstant.OtpHeader, out string keyFIleName))
        {
            Console.WriteLine("Message has no OTP key header.");
            return;
        }

        byte[] key;
        var keyPath = KeyManager.GetKeyFilePath(keyFIleName);
        var (resolvedPath, found) = KeyManager.ResolveKeyFilePath(keyPath);
        if (!found)
        {
            Console.WriteLine("Key file not found.");
            return;
        }

        keyPath = resolvedPath;
        key = System.IO.File.ReadAllBytes(keyPath);
        var decrypted = _otp.Decrypt(msg.TextBody, key);

        Console.WriteLine("------ Decrypted Message ------");
        Console.WriteLine(decrypted);
        Console.WriteLine("-------------------------------");
    }
}
