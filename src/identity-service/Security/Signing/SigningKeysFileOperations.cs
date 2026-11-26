using System.Text.Json;

namespace IdentityService.Security.Signing;

internal static class SigningKeysFileOperations
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static SigningKeySetFile Load(string path)
    {
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<SigningKeySetFile>(json, SerializerOptions)
            ?? throw new InvalidDataException("Signing keys file is empty or invalid.");
        return document;
    }

    internal static void SaveAtomic(string path, SigningKeySetFile document)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        var tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}