namespace OtpMailApp.Shared;

public class FileManager
{
    // read the content of a file by path
    public static async Task<string> ReadFile(string path, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(path);
            return await reader.ReadToEndAsync(ct);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file at {path}: {ex.Message}");
        }
    }

    public static async Task<byte[]> ReadFileBytes(string path, CancellationToken ct)
    {
        try
        {
            return await File.ReadAllBytesAsync(path, ct);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file bytes at {path}: {ex.Message}");
        }
    }

    // write content to a file by path
    public static async Task WriteFile(string path, string content, CancellationToken ct)
    {
        try
        {
            using var writer = new StreamWriter(path, false);
            await writer.WriteAsync(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error writing file at {path}: {ex.Message}");
        }
    }
}
