using System.Text.Json;
using System.Windows;
using AudioAnchor.Core;
using AudioAnchor.Windows;

namespace AudioAnchor.App;

public partial class App : Application
{
    private SingleInstance? _instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var demo = e.Args.Contains("--demo");
        string? Value(string flag) { var index = Array.IndexOf(e.Args, flag); return index >= 0 && index + 1 < e.Args.Length ? e.Args[index + 1] : null; }
        var dataDirectory = Value("--data-dir") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), demo ? "AudioAnchor-Demo" : "AudioAnchor");
        if (Value("--diagnose") is { } diagnosticPath)
        {
            try
            {
                using var audio = new WindowsAudioBackend();
                audio.RefreshNotifications();
                File.WriteAllText(diagnosticPath, JsonSerializer.Serialize(new { Devices = audio.GetDevices(), Defaults = new AppSettings().Roles().Select(r => new { r.Slot.Flow, r.Slot.Role, Id = audio.GetDefault(r.Slot) }) }, new JsonSerializerOptions { WriteIndented = true }));
                Shutdown(0);
            }
            catch (Exception ex) { File.WriteAllText(diagnosticPath, ex.ToString()); Shutdown(1); }
            return;
        }
        try
        {
            _instance = new SingleInstance(dataDirectory);
            if (!_instance.IsFirst)
            {
                _instance.Send(e.Args.Contains("--exit") ? "exit" : "show");
                Shutdown();
                return;
            }
            if (e.Args.Contains("--exit")) { Shutdown(); return; }
            Directory.CreateDirectory(dataDirectory);
            var window = new MainWindow(demo, dataDirectory);
            MainWindow = window;
            _instance.Listen(command => Dispatcher.BeginInvoke(() => { if (command == "exit") window.ExitApplication(); else window.ShowSettings(); }));
            if (!e.Args.Contains("--background")) window.ShowSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AudioAnchor could not start.\n\n{ex.Message}", "AudioAnchor", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { _instance?.Dispose(); base.OnExit(e); }
}
