using OtpMailApp.Cipher;
using OtpMailApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands;

public class DecryptCommand : ICommand
{
    private readonly ICipherService _otp;

    public DecryptCommand(ICipherService otp)
    {
        _otp = otp;
    }

    public void Execute()
    {
        Console.Write("Enter Base64 encrypted text: ");
        string encrypted = Console.ReadLine() ?? "";

        Console.Write("Enter key file path: ");
        string path = Console.ReadLine() ?? "";
        var (resolvedPath, found) = KeyManager.ResolveKeyFilePath(path);
        if (!found)
        {
            Console.WriteLine("Key file not found.");
            return;
        }

        path = resolvedPath;
        byte[] key = System.IO.File.ReadAllBytes(path);

        string decrypted = _otp.Decrypt(encrypted, key);
        Console.WriteLine($"Decrypted (UTF-8): {decrypted}");
    }
}
