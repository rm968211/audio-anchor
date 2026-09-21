using AudioAnchor.Core;

namespace AudioAnchor.Tests;

public sealed class SettingsMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AudioAnchor-migration-tests", Guid.NewGuid().ToString("N"));
    private string Legacy => Path.Combine(_root, "legacy");
    private string New => Path.Combine(_root, "new");

    [Fact]
    public void CopiesLegacySettingsIntoNewDirectory()
    {
        Directory.CreateDirectory(Legacy);
        File.WriteAllText(Path.Combine(Legacy, "settings.json"), "{\"SchemaVersion\":1}");
        Assert.True(SettingsMigration.MigrateSettingsFile(Legacy, New));
        Assert.Equal("{\"SchemaVersion\":1}", File.ReadAllText(Path.Combine(New, "settings.json")));
    }
    [Fact]
    public void DoesNothingWhenNewDirectoryAlreadyExists()
    {
        Directory.CreateDirectory(Legacy);
        File.WriteAllText(Path.Combine(Legacy, "settings.json"), "{}");
        Directory.CreateDirectory(New);
        Assert.False(SettingsMigration.MigrateSettingsFile(Legacy, New));
        Assert.False(File.Exists(Path.Combine(New, "settings.json")));
    }
    [Fact]
    public void DoesNothingWhenNoLegacySettingsExist()
    {
        Assert.False(SettingsMigration.MigrateSettingsFile(Legacy, New));
        Assert.False(Directory.Exists(New));
    }
    [Fact]
    public void MigratedSettingsLoadCorrectly()
    {
        Directory.CreateDirectory(Legacy);
        var store = new SettingsStore(Legacy);
        store.Save(TestAudio.All);
        Assert.True(SettingsMigration.MigrateSettingsFile(Legacy, New));
        Assert.Equal(TestAudio.All, new SettingsStore(New).Load().Settings);
    }
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
