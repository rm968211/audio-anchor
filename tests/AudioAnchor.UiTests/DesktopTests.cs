using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AudioAnchor.UiTests;

public sealed class DesktopTests
{
    private static string FindExecutable()
    {
        if (Environment.GetEnvironmentVariable("AUDIOANCHOR_EXE") is { } specified) return specified;
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AudioAnchor.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var config = AppContext.BaseDirectory.Contains("Release") ? "Release" : "Debug";
        return Path.Combine(directory.FullName, "src", "AudioAnchor.App", "bin", config, "net10.0-windows", "AudioAnchor.exe");
    }
    private static void Until(Func<bool> condition)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(15))
        {
            try { if (condition()) return; }
            catch (Exception ex) when (ex is IOException or JsonException or FlaUI.Core.Exceptions.ElementNotAvailableException) { }
            Thread.Sleep(100);
        }
        Assert.Fail("Timed out waiting for the desktop application.");
    }
    private static Process Start(string exe, string args) => Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden })!;
    private static JsonElement Status(string data) { using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(data, "demo-status.json"))); return json.RootElement.Clone(); }

    [Fact, Trait("Category", "UI")]
    public void SelectApplyPauseRestorePersistAndCloseToTray()
    {
        var exe = FindExecutable();
        var data = Path.Combine(Path.GetTempPath(), "AudioAnchor-UI", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        var arguments = $"--demo --data-dir \"{data}\"";
        Application? app = null;
        using var automation = new UIA3Automation();
        try
        {
            app = Application.Launch(exe, arguments);
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.NotNull(window);
            ComboBox Choice(string id) => window!.FindFirstDescendant(cf => cf.ByAutomationId(id))!.AsComboBox()!;
            Button Button(string id) => window!.FindFirstDescendant(cf => cf.ByAutomationId(id))!.AsButton()!;
            Choice("PlaybackChoice").Select("Desk speakers");
            Choice("CallPlaybackChoice").Select("USB headset");
            Choice("RecordingChoice").Select("Desk microphone");
            Choice("CallRecordingChoice").Select("USB headset microphone");
            Button("ApplyButton").Invoke();
            Until(() => Status(data).GetProperty("Slots").EnumerateArray().All(s => s.GetProperty("State").GetInt32() == 2));
            using (var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(data, "settings.json"))))
                Assert.Equal("demo-speakers", settings.RootElement.GetProperty("Playback").GetProperty("Id").GetString());

            Button("PauseButton").Invoke();
            Until(() => Status(data).GetProperty("Slots").EnumerateArray().All(s => s.GetProperty("State").GetInt32() == 1));
            Button("SimulateButton").Invoke();
            Until(() => Status(data).GetProperty("Slots")[0].GetProperty("CurrentId").GetString() == "demo-headset");
            Button("PauseButton").Invoke();
            Until(() => Status(data).GetProperty("Slots")[0].GetProperty("CurrentId").GetString() == "demo-speakers");
            Button("SimulateButton").Invoke();
            Until(() => Status(data).GetProperty("Slots").EnumerateArray().All(s => s.GetProperty("State").GetInt32() == 2 && s.GetProperty("CurrentId").GetString() == s.GetProperty("PreferredId").GetString()));

            window.Close();
            Assert.False(app.HasExited);
            using (var second = Start(exe, arguments)) Assert.True(second.WaitForExit(5000));
            Until(() => app.GetMainWindow(automation, TimeSpan.FromSeconds(1))?.IsAvailable == true);
            using (var quit = Start(exe, arguments + " --exit")) Assert.True(quit.WaitForExit(5000));
            Until(() => app.HasExited);
            app.Dispose();
            app = Application.Launch(exe, arguments);
            window = app.GetMainWindow(automation, TimeSpan.FromSeconds(15));
            Assert.Equal("Desk speakers", Choice("PlaybackChoice").SelectedItem!.Text);
            Until(() => Status(data).GetProperty("Slots").EnumerateArray().All(s => s.GetProperty("State").GetInt32() == 2));
        }
        catch
        {
            var results = Environment.GetEnvironmentVariable("AUDIOANCHOR_TEST_RESULTS") ?? Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
            Directory.CreateDirectory(results);
            try { using var bitmap = app?.GetMainWindow(automation)?.Capture(); bitmap?.Save(Path.Combine(results, "ui-failure.png")); } catch { }
            throw;
        }
        finally
        {
            if (app is not null)
            {
                if (!app.HasExited)
                {
                    using var quit = Start(exe, arguments + " --exit");
                    quit.WaitForExit(5000);
                    var deadline = Stopwatch.StartNew();
                    while (!app.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(5)) Thread.Sleep(100);
                    if (!app.HasExited) app.Kill();
                }
                app.Dispose();
            }
            // Remove only this test's unique data directory after the app exits.
            if (Directory.Exists(data)) Directory.Delete(data, true);
        }
    }
}
