using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
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
    private readonly System.Drawing.Icon _trayIcon;
    private readonly Forms.ToolStripMenuItem _pauseMenu;
    private readonly GitHubReleaseSource? _updateSource;
    private bool _exiting;
    private bool _choicesInitialized;
    private string? _lastError;
    private string? _updateUrl;
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
        _trayIcon = LoadTrayIcon();
        _tray = new() { Icon = _trayIcon, Text = "SoundAnchor", ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowSettings();
        StartupCheck.IsEnabled = !demo;
        try { StartupCheck.IsChecked = !demo && StartupRegistration.Enabled; }
        catch (Exception ex) { WarningText.Text = "Startup setting could not be read: " + ex.Message; }
        if (demo) { Title = "SoundAnchor — Demo"; SimulateButton.Visibility = Visibility.Visible; }
        RefreshChoices(true);
        UpdatePause();
        _worker.Request();
        SystemEvents.PowerModeChanged += PowerChanged;
        // Demo mode stays offline and network-free, matching its isolation promise; UI tests run in demo mode.
        if (!demo)
        {
            _updateSource = new("rm968211", "sound-anchor");
            _ = CheckForUpdateAsync();
        }
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        // Same embedded resource as the window's XAML Icon, so the tray and title bar always match.
        using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/icon.ico"))!.Stream;
        return new System.Drawing.Icon(stream, new System.Drawing.Size(32, 32));
    }
    private static readonly Version CurrentVersion = ReadCurrentVersion();
    private static Version ReadCurrentVersion()
    {
        var informational = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var numeric = informational?.Split('+')[0];
        return numeric is not null && Version.TryParse(numeric, out var version) ? version : new Version(0, 0, 0);
    }
    private async Task CheckForUpdateAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var release = await UpdateChecker.CheckAsync(_updateSource!, CurrentVersion, timeout.Token);
        if (release is null || _exiting) return;
        _updateUrl = release.Url;
        UpdateBannerText.Text = $"SoundAnchor {release.Version} is available — you have {CurrentVersion}.";
        UpdateBanner.Visibility = Visibility.Visible;
    }
    private void UpdateBannerClicked(object sender, RoutedEventArgs e)
    {
        if (_updateUrl is null) return;
        try { Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { ShowError(ex); }
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
    private void SoundControlPanelClicked(object sender, RoutedEventArgs e)
    {
        // The classic Sound control panel still owns per-role defaults, so open that rather than Settings.
        try { Process.Start(new ProcessStartInfo("control.exe", "mmsys.cpl,,0") { UseShellExecute = true })?.Dispose(); }
        catch (Exception ex) { ShowError(ex); }
    }
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
        string Name(string? id) => id is null ? "no device" : _devices.FirstOrDefault(d => d.Id == id)?.Name ?? "a device that is no longer available";
        void Status(TextBlock target, AudioFlow flow, params AudioRole[] roles)
        {
            // Console and Multimedia share one selection, so describe the slot that needs attention most.
            var slot = report.Slots.Where(s => s.Slot.Flow == flow && roles.Contains(s.Slot.Role)).OrderByDescending(s => Attention(s.State)).FirstOrDefault();
            target.Text = slot is null ? "" : slot.State switch
            {
                SlotState.Error => $"Windows would not accept this choice — {slot.Error}",
                SlotState.Unmanaged => $"Not protected. Windows is using {Name(slot.CurrentId)} and may change it at any time.",
                SlotState.Paused => $"Protection paused. Windows is using {Name(slot.CurrentId)}.",
                SlotState.Waiting => $"Not connected right now, so Windows is using {Name(slot.CurrentId)}. SoundAnchor switches back as soon as it returns.",
                _ => "In use now. SoundAnchor puts it back if Windows changes it.",
            };
        }
        Status(PlaybackStatus, AudioFlow.Playback, AudioRole.Console, AudioRole.Multimedia);
        Status(CallPlaybackStatus, AudioFlow.Playback, AudioRole.Communications);
        Status(RecordingStatus, AudioFlow.Recording, AudioRole.Console, AudioRole.Multimedia);
        Status(CallRecordingStatus, AudioFlow.Recording, AudioRole.Communications);
        if (report.HasErrors && report.Summary != _lastError) Log(report.Summary + " " + string.Join("; ", report.Slots.Where(s => s.Error is not null).Select(s => s.Error)));
        _lastError = report.HasErrors ? report.Summary : null;
        if (_demo) File.WriteAllText(Path.Combine(_dataDirectory, "demo-status.json"), JsonSerializer.Serialize(report));
    });
    private static int Attention(SlotState state) => state switch { SlotState.Error => 4, SlotState.Waiting => 3, SlotState.Paused => 2, SlotState.Unmanaged => 1, _ => 0 };
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
        _updateSource?.Dispose();
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Dispose();
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }
}
