using OtpMailApp.Cipher;
using OtpMailApp.Shared;

namespace OtpMailApp.Commands;

public class GenerateKeyCommand : ICommand
{
    private readonly ICipherService _otp;

    public GenerateKeyCommand(ICipherService otp)
    {
        _otp = otp;
    }

    public void Execute()
    {
        Console.Write("Enter desired key length (number of bytes): ");
        if (!int.TryParse(Console.ReadLine(), out int length))
        {
            Console.WriteLine("Invalid number.");
            return;
        }

        byte[] key = _otp.GenerateRandomKey(length);

        var path = KeyManager.GetKeyFilePath();
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
}
