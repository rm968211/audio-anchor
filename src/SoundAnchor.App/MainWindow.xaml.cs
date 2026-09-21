using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SoundAnchor.Core;
using SoundAnchor.Windows;
using Forms = System.Windows.Forms;

namespace SoundAnchor.App;

public partial class MainWindow : Window
{
    private readonly bool _demo;
    private readonly string _dataDirectory;
    private readonly SettingsStore _store;
    private AppSettings _settings;
    private readonly IAudioBackend _backend;
    private readonly EnforcementWorker _worker;
    private readonly Forms.NotifyIcon _tray;
    private readonly Forms.ToolStripMenuItem _pauseMenu;
    private bool _exiting;
    private bool _choicesInitialized;
    private string? _lastError;
    private IReadOnlyList<AudioDevice> _devices = [];
    private sealed record Choice(string? Id, string Name, string Display) { public override string ToString() => Display; }

    public MainWindow(bool demo, string dataDirectory)
    {
        InitializeComponent();
        _demo = demo;
        _dataDirectory = dataDirectory;
        _store = new(dataDirectory);
        var loaded = _store.Load();
        _settings = loaded.Settings;
        WarningText.Text = loaded.Warning ?? (demo ? "Demo mode — your real audio devices and startup settings are untouched." : "");
        _backend = demo ? new SimulatedAudioBackend() : new WindowsAudioBackend();
        _worker = new(_backend, _settings);
        _worker.Updated += OnUpdated;
        _pauseMenu = new("Pause protection", null, (_, _) => Dispatcher.BeginInvoke(TogglePause));
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => Dispatcher.BeginInvoke(ShowSettings));
        menu.Items.Add(_pauseMenu);
        menu.Items.Add("Restore now", null, (_, _) => _worker.Request());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.BeginInvoke(ExitApplication));
        _tray = new() { Icon = System.Drawing.SystemIcons.Application, Text = "SoundAnchor", ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowSettings();
        StartupCheck.IsEnabled = !demo;
        try { StartupCheck.IsChecked = !demo && StartupRegistration.Enabled; }
        catch (Exception ex) { WarningText.Text = "Startup setting could not be read: " + ex.Message; }
        if (demo) { Title = "SoundAnchor — Demo"; SimulateButton.Visibility = Visibility.Visible; }
        RefreshChoices(true);
        UpdatePause();
        _worker.Request();
        SystemEvents.PowerModeChanged += PowerChanged;
    }

    private void RefreshChoices(bool initial)
    {
        try { _devices = _backend.GetDevices(); }
        catch (Exception ex) { WarningText.Text = ex.Message; return; }
        initial |= !_choicesInitialized;
        Fill(PlaybackChoice, AudioFlow.Playback, initial ? _settings.Playback : Preference(PlaybackChoice));
        Fill(CallPlaybackChoice, AudioFlow.Playback, initial ? _settings.CommunicationsPlayback : Preference(CallPlaybackChoice));
        Fill(RecordingChoice, AudioFlow.Recording, initial ? _settings.Recording : Preference(RecordingChoice));
        Fill(CallRecordingChoice, AudioFlow.Recording, initial ? _settings.CommunicationsRecording : Preference(CallRecordingChoice));
        _choicesInitialized = true;
    }
    private void Fill(ComboBox combo, AudioFlow flow, DevicePreference? selected)
    {
        if (combo.IsDropDownOpen) return;
        var choices = new List<Choice> { new(null, "Unmanaged", "Let Windows choose (unmanaged)") };
        foreach (var device in _devices.Where(d => d.Flow == flow && d.Available).OrderBy(d => d.Name))
        {
            var duplicate = _devices.Count(d => d.Name == device.Name && d.Flow == flow && d.Available) > 1;
            var suffix = duplicate ? $" [{device.Id[^Math.Min(9, device.Id.Length)..]}]" : "";
            choices.Add(new(device.Id, device.Name, device.Name + suffix));
        }
        if (selected is not null && choices.All(c => c.Id != selected.Id)) choices.Add(new(selected.Id, selected.Name, selected.Name + " (disconnected)"));
        combo.ItemsSource = choices;
        combo.SelectedItem = choices.First(c => c.Id == selected?.Id);
    }
    private static DevicePreference? Preference(ComboBox combo) => combo.SelectedItem is Choice { Id: { } id } choice ? new(id, choice.Name) : null;
    private void Save(AppSettings settings)
    {
        _store.Save(settings);
        _settings = settings;
        _worker.Configure(settings);
        UpdatePause();
    }
    private void ApplyClicked(object sender, RoutedEventArgs e)
    {
        try { Save(_settings with { Playback = Preference(PlaybackChoice), CommunicationsPlayback = Preference(CallPlaybackChoice), Recording = Preference(RecordingChoice), CommunicationsRecording = Preference(CallRecordingChoice) }); }
        catch (Exception ex) { ShowError(ex); }
    }
    private void PauseClicked(object sender, RoutedEventArgs e) => TogglePause();
    private void TogglePause() { try { Save(_settings with { Paused = !_settings.Paused }); } catch (Exception ex) { ShowError(ex); } }
    private void UpdatePause()
    {
        PauseButton.Content = _settings.Paused ? "Resume" : "Pause";
        _pauseMenu.Text = _settings.Paused ? "Resume protection" : "Pause protection";
    }
    private void RestoreClicked(object sender, RoutedEventArgs e) => _worker.Refresh();
    private void SimulateClicked(object sender, RoutedEventArgs e) => ((SimulatedAudioBackend)_backend).SimulateSwitch();
    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        if (_demo) return;
        try { StartupRegistration.Set(StartupCheck.IsChecked == true); }
        catch (Exception ex) { StartupCheck.IsChecked = !StartupCheck.IsChecked; ShowError(ex); }
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) { if (e.Mode == PowerModes.Resume) _worker.Refresh(); }
    private void OnUpdated(EnforcementReport report) => Dispatcher.BeginInvoke(() =>
    {
        if (_exiting) return;
        StatusText.Text = report.Summary;
        _tray.Text = _settings.Paused ? "SoundAnchor — paused" : report.HasErrors ? "SoundAnchor — needs attention" : "SoundAnchor — protecting audio";
        RefreshChoices(false);
        string Name(string? id) => id is null ? "None" : _devices.FirstOrDefault(d => d.Id == id)?.Name ?? "Unavailable device";
        void Status(TextBlock target, AudioFlow flow, params AudioRole[] roles)
        {
            var slots = report.Slots.Where(s => s.Slot.Flow == flow && roles.Contains(s.Slot.Role)).ToArray();
            target.Text = string.Join(" · ", slots.Select(s => $"{s.Slot.Role}: {Name(s.CurrentId)} ({s.State}){(s.Error is null ? "" : " — " + s.Error)}"));
        }
        Status(PlaybackStatus, AudioFlow.Playback, AudioRole.Console, AudioRole.Multimedia);
        Status(CallPlaybackStatus, AudioFlow.Playback, AudioRole.Communications);
        Status(RecordingStatus, AudioFlow.Recording, AudioRole.Console, AudioRole.Multimedia);
        Status(CallRecordingStatus, AudioFlow.Recording, AudioRole.Communications);
        if (report.HasErrors && report.Summary != _lastError) Log(report.Summary + " " + string.Join("; ", report.Slots.Where(s => s.Error is not null).Select(s => s.Error)));
        _lastError = report.HasErrors ? report.Summary : null;
        if (_demo) File.WriteAllText(Path.Combine(_dataDirectory, "demo-status.json"), JsonSerializer.Serialize(report));
    });
    private void Log(string message)
    {
        try
        {
            var path = Path.Combine(_dataDirectory, "diagnostics.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) File.Move(path, path + ".old", true);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private void ShowError(Exception ex) { Log(ex.ToString()); MessageBox.Show(this, ex.Message, "SoundAnchor", MessageBoxButton.OK, MessageBoxImage.Error); }
    public void ShowSettings() { Show(); WindowState = WindowState.Normal; Activate(); }
    protected override void OnClosing(CancelEventArgs e) { if (!_exiting) { e.Cancel = true; Hide(); } base.OnClosing(e); }
    public async void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        SystemEvents.PowerModeChanged -= PowerChanged;
        _worker.Updated -= OnUpdated;
        await _worker.DisposeAsync();
        _backend.Dispose();
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        Application.Current.Shutdown();
    }
}
