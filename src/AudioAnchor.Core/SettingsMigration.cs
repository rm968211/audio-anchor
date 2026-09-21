namespace AudioAnchor.Core;

public static class SettingsMigration
{
    // Copies a legacy install's settings.json into a not-yet-created data directory, for users
    // upgrading from the app's previous name. No-op once the new directory exists (already
    // migrated, or a fresh install already initialized it) or if there is nothing to migrate.
    public static bool MigrateSettingsFile(string legacyDirectory, string newDirectory)
    {
        if (Directory.Exists(newDirectory)) return false;
        var legacyFile = Path.Combine(legacyDirectory, "settings.json");
        if (!File.Exists(legacyFile)) return false;
        Directory.CreateDirectory(newDirectory);
        File.Copy(legacyFile, Path.Combine(newDirectory, "settings.json"));
        return true;
    }
}
