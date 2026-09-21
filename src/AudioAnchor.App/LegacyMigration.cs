using Microsoft.Win32;
using AudioAnchor.Core;

namespace AudioAnchor.App;

// One-time best-effort migration for users upgrading from SoundAnchor, this app's previous name.
// Covers both the installer and portable-ZIP upgrade paths, since only app code runs for both;
// the installer's own SoundAnchor.exe/registry cleanup (installer/AudioAnchor.iss) only covers
// the installed-via-Inno path. Idempotent: after the first successful run the new data directory
// exists, which is itself the guard against re-migrating on every launch.
internal static class LegacyMigration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Run(string dataDirectory)
    {
        try
        {
            var legacyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundAnchor");
            SettingsMigration.MigrateSettingsFile(legacyDirectory, dataDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        try { MigrateStartup(); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException) { }
    }

    private static void MigrateStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue("SoundAnchor") is not string) return;
        key.DeleteValue("SoundAnchor", false);
        // Carry the enabled state forward under the new name/path; StartupRegistration.Enabled
        // only ever looks at "AudioAnchor", so without this the migrated user's choice is lost.
        if (key.GetValue("AudioAnchor") is null) StartupRegistration.Set(true);
    }
}
