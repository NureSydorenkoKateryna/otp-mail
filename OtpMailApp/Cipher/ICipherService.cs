namespace OtpMailApp.Cipher;

public interface ICipherService
{
    string Decrypt(string cipherTextBase64, byte[] key);
    string Encrypt(string plainText, byte[] key);
    byte[] GenerateRandomKey(int length);
}
