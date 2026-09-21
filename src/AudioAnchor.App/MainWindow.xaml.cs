using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using AudioAnchor.Core;
using AudioAnchor.Windows;
using Forms = System.Windows.Forms;

namespace AudioAnchor.App;

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
        SetWarning(loaded.Warning ?? (demo ? "Demo mode — your real audio devices and startup settings are untouched." : null));
        _backend = demo ? new SimulatedAudioBackend() : new WindowsAudioBackend();
        _worker = new(_backend, _settings);
        _worker.Updated += OnUpdated;
        _pauseMenu = new("Pause protection", null, (_, _) => Dispatcher.BeginInvoke(TogglePause));
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => Dispatcher.BeginInvoke(ShowSettings));
        // Demo mode stays offline; the update source below is never created for it, so there is nothing to check.
        if (!demo) menu.Items.Add("Check for update", null, (_, _) => Dispatcher.BeginInvoke(() => { ShowSettings(); _ = CheckForUpdateAsync(manual: true); }));
        menu.Items.Add("About", null, (_, _) => Dispatcher.BeginInvoke(ShowAbout));
        menu.Items.Add(_pauseMenu);
        menu.Items.Add("Restore now", null, (_, _) => _worker.Request());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.BeginInvoke(ExitApplication));
        _trayIcon = LoadTrayIcon();
        _tray = new() { Icon = _trayIcon, Text = "AudioAnchor", ContextMenuStrip = menu, Visible = true };
        _tray.DoubleClick += (_, _) => ShowSettings();
        StartupCheck.IsEnabled = !demo;
        try { StartupCheck.IsChecked = !demo && StartupRegistration.Enabled; }
        catch (Exception ex) { SetWarning("Startup setting could not be read: " + ex.Message); }
        if (demo) { Title = "AudioAnchor — Demo"; SimulateButton.Visibility = Visibility.Visible; }
        RefreshChoices(true);
        UpdatePause();
        _worker.Request();
        SystemEvents.PowerModeChanged += PowerChanged;
        // Demo mode stays offline and network-free, matching its isolation promise; UI tests run in demo mode.
        if (!demo)
        {
            _updateSource = new("rm968211", "audio-anchor");
            _ = CheckForUpdateAsync();
        }
    }

    private void SetWarning(string? text)
    {
        // Collapsed rather than an empty string so the card doesn't reserve a blank line's height.
        WarningText.Text = text ?? "";
        WarningText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
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
    private async Task CheckForUpdateAsync(bool manual = false)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var release = await UpdateChecker.CheckAsync(_updateSource!, CurrentVersion, timeout.Token);
        if (_exiting) return;
        if (release is null)
        {
            // The automatic startup check stays silent either way; only a manual click confirms
            // the "nothing to do" outcome, since silence there reads as the button not working.
            if (manual) new InfoDialog($"You're up to date. AudioAnchor {CurrentVersion} is the latest version.").ShowDialog();
            return;
        }
        _updateUrl = release.Url;
        UpdateBannerText.Text = $"AudioAnchor {release.Version} is available. You have {CurrentVersion}.";
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
        catch (Exception ex) { SetWarning(ex.Message); return; }
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
        StatusCard.Background = StatusCardBrush(report);
        _tray.Text = _settings.Paused ? "AudioAnchor — paused" : report.HasErrors ? "AudioAnchor — needs attention" : "AudioAnchor — protecting audio";
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
                SlotState.Waiting => $"Not connected right now, so Windows is using {Name(slot.CurrentId)}. AudioAnchor switches back as soon as it returns.",
                _ => "In use now. AudioAnchor puts it back if Windows changes it.",
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
    private static readonly Brush ProtectedStatusBrush = Frozen(Color.FromRgb(0x1B, 0x3A, 0x2E));
    private static readonly Brush CautionStatusBrush = Frozen(Color.FromRgb(0x3D, 0x2E, 0x12));
    private static readonly Brush ErrorStatusBrush = Frozen(Color.FromRgb(0x3D, 0x14, 0x14));
    private static Brush Frozen(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
    private Brush StatusCardBrush(EnforcementReport report)
    {
        // Mirrors EnforcementReport.Summary's own precedence, so the tint always matches the text.
        if (report.HasErrors) return ErrorStatusBrush;
        if (report.Slots.Any(s => s.State is SlotState.Paused or SlotState.Waiting)) return CautionStatusBrush;
        if (report.Slots.Any(s => s.State == SlotState.Protected)) return ProtectedStatusBrush;
        return (Brush)FindResource("CardBackgroundFillColorDefaultBrush");
    }
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
    private void ShowError(Exception ex) { Log(ex.ToString()); MessageBox.Show(this, ex.Message, "AudioAnchor", MessageBoxButton.OK, MessageBoxImage.Error); }
    public void ShowSettings() { Show(); WindowState = WindowState.Normal; Activate(); }
    // No Owner: MainWindow may not have a window handle yet if launched with --background
    // and Settings was never opened, which WPF requires before it can own another window.
    private void ShowAbout() => new AboutWindow(CurrentVersion).ShowDialog();
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
