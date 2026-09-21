using System.Text.Json;

namespace AudioAnchor.Core;

public sealed record SettingsLoadResult(AppSettings Settings, string? Warning = null);

public sealed class SettingsStore(string directory)
{
    public string FilePath { get; } = Path.Combine(directory, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public SettingsLoadResult Load()
    {
        if (!File.Exists(FilePath)) return new(new());
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options)
                ?? throw new JsonException("Empty settings");
            if (settings.SchemaVersion != 1) throw new JsonException("Unsupported settings version");
            if (settings.Roles().Any(r => r.Preference is { } p && (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name))))
                throw new JsonException("Invalid device preference");
            return new(settings);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new(new() { Paused = true }, $"Preferences could not be loaded. Enforcement is paused. {ex.Message}");
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        // Preserve any unreadable/future-version configuration before an explicit replacement.
        if (File.Exists(FilePath) && Load().Warning is not null)
            File.Copy(FilePath, FilePath + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, FilePath, true);
    }
}
