using AudioAnchor.Core;

namespace AudioAnchor.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "AudioAnchor-tests", Guid.NewGuid().ToString("N"));
    [Fact]
    public void FirstRunHasNoManagedDevices()
    {
        var loaded = new SettingsStore(_directory).Load();
        Assert.Null(loaded.Warning);
        Assert.All(loaded.Settings.Roles(), r => Assert.Null(r.Preference));
    }
    [Fact]
    public void PreferencesAndPauseSurviveRestart()
    {
        var store = new SettingsStore(_directory);
        var settings = TestAudio.All with { Paused = true };
        store.Save(settings);
        Assert.Equal(settings, new SettingsStore(_directory).Load().Settings);
        Assert.False(File.Exists(store.FilePath + ".tmp"));
    }
    [Theory]
    [InlineData("broken json")]
    [InlineData("null")]
    [InlineData("{\"SchemaVersion\":999}")]
    [InlineData("{\"Playback\":{\"Id\":\"\",\"Name\":\"x\"}}")]
    public void InvalidOrFutureSettingsPauseAndPreserveOriginal(string contents)
    {
        Directory.CreateDirectory(_directory);
        var store = new SettingsStore(_directory);
        File.WriteAllText(store.FilePath, contents);
        var result = store.Load();
        Assert.True(result.Settings.Paused);
        Assert.NotNull(result.Warning);
        Assert.Equal(contents, File.ReadAllText(store.FilePath));
        store.Save(TestAudio.All);
        var backup = Assert.Single(Directory.GetFiles(_directory, "*.backup-*"));
        Assert.Equal(contents, File.ReadAllText(backup));
    }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
