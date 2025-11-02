using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace OtpMailApp.Cipher;

public class OtpCipherService : ICipherService
{
    private  byte[] NormalizeKey(byte[] key, int targetLength)
    {
        if (key.Length == targetLength)
            return key;

        if (key.Length > targetLength)
        {
            // Trim key
            byte[] trimmed = new byte[targetLength];
            Array.Copy(key, trimmed, targetLength);
            return trimmed;
        }

        byte[] extended = new byte[targetLength];
        for (int i = 0; i < targetLength; i++)
            extended[i] = key[i % key.Length];

        return extended;
    }

    public string Encrypt(string plainText, byte[] key)
    {
        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        key = NormalizeKey(key, plainBytes.Length);

        if (plainBytes.Length != key.Length)
            throw new ArgumentException("Plaintext and key must be of the same length.");

        byte[] cipherBytes = new byte[plainBytes.Length];
        for (int i = 0; i < plainBytes.Length; i++)
            cipherBytes[i] = (byte)(plainBytes[i] ^ key[i]);

        return Convert.ToBase64String(cipherBytes);
    }

    public  string Decrypt(string cipherTextBase64, byte[] key)
    {
        if (cipherTextBase64 == null)
            throw new ArgumentNullException(nameof(cipherTextBase64));
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        byte[] cipherBytes = Convert.FromBase64String(cipherTextBase64);
        key = NormalizeKey(key, cipherBytes.Length);

        if (cipherBytes.Length != key.Length)
            throw new ArgumentException("Ciphertext and key must be of the same length.");

        byte[] plainBytes = new byte[cipherBytes.Length];
        for (int i = 0; i < cipherBytes.Length; i++)
            plainBytes[i] = (byte)(cipherBytes[i] ^ key[i]);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public byte[] GenerateRandomKey(int length)
    {
        byte[] key = new byte[length];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }
        return key;
    }
}
