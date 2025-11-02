using System.IO;

namespace OtpMailApp.Shared;

public class KeyManager
{
    private const string KeysDirectory = "keys";
    public static string GetKeyFileName()
        => $"key_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";
    public static string GetKeyFilePath(string fileName)
        => Path.Combine(KeysDirectory, fileName);
    public static string GetKeyFilePath()
        => Path.Combine(KeysDirectory, GetKeyFileName());
    public static void EnsureKeysDirectoryExists()
    {
        if (!Directory.Exists(KeysDirectory))
        {
            Directory.CreateDirectory(KeysDirectory);
        }
    }

    public static (string, bool) ResolveKeyFilePath(string inputPath)
    {
        if (File.Exists(inputPath))
        {
            return (inputPath, true);
        }

        var combinedPath = GetKeyFilePath(inputPath);
        if (File.Exists(combinedPath))
        {
            return (combinedPath, true);
        }

        var fileName = Path.GetFileName(inputPath);
        combinedPath = GetKeyFilePath(fileName);
        if (File.Exists(combinedPath))
        {
            return (combinedPath, true);
        }

        return (string.Empty, false);
    }

}
