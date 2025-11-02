using OtpMailApp.Cipher;
using OtpMailApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtpMailApp.Commands
{
    public class EncryptCommand : ICommand
    {
        private readonly ICipherService _otp;

        public EncryptCommand(ICipherService otp)
        {
            _otp = otp;
        }

        public void Execute()
        {
            Console.Write("Enter the message to encrypt: ");
            string message = Console.ReadLine() ?? "";

            Console.Write("Enter key file path or enter key file name (or leave empty to generate a random key): ");
            string path = Console.ReadLine() ?? "";

            var (resolvedPath, found) = KeyManager.ResolveKeyFilePath(path);
            if (!found)
            {
                Console.WriteLine("Key file not found. A random key will be generated.");
            }
            byte[] key;
            if (found)
            {
                path = resolvedPath;
                key = System.IO.File.ReadAllBytes(path);
                Console.WriteLine("Key loaded from file.");
            }
            else  
            {
                key = _otp.GenerateRandomKey(Encoding.UTF8.GetByteCount(message));
                path = KeyManager.GetKeyFilePath();
                KeyManager.EnsureKeysDirectoryExists();
                System.IO.File.WriteAllBytes(path, key);
                Console.WriteLine($"Random key generated and saved to {path}");
            }
            
            string encrypted = _otp.Encrypt(message, key);
            Console.WriteLine($"Encrypted (Base64): {encrypted}");
        }
    }
}
