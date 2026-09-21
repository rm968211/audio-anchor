namespace SoundAnchor.Core;

// Explicitly selected by --demo; never connected to Windows audio or startup settings.
public sealed class SimulatedAudioBackend : IAudioBackend
{
    private readonly object _gate = new();
    private readonly Dictionary<AudioSlot, string?> _defaults = [];
    private readonly List<AudioDevice> _devices =
    [
        new("demo-speakers", "Desk speakers", AudioFlow.Playback),
        new("demo-headset", "USB headset", AudioFlow.Playback),
        new("demo-microphone", "Desk microphone", AudioFlow.Recording),
        new("demo-headset-mic", "USB headset microphone", AudioFlow.Recording)
    ];
    public event Action? Changed;
    public SimulatedAudioBackend()
    {
        foreach (var (slot, _) in new AppSettings().Roles())
            _defaults[slot] = slot.Flow == AudioFlow.Playback ? "demo-headset" : "demo-headset-mic";
    }
    public IReadOnlyList<AudioDevice> GetDevices() { lock (_gate) return _devices.ToArray(); }
    public string? GetDefault(AudioSlot slot) { lock (_gate) return _defaults.GetValueOrDefault(slot); }
    public void SetDefault(AudioSlot slot, string deviceId)
    {
        bool changed;
        lock (_gate) { changed = _defaults.GetValueOrDefault(slot) != deviceId; _defaults[slot] = deviceId; }
        if (changed) Changed?.Invoke();
    }
    public void SimulateSwitch()
    {
        foreach (var (slot, _) in new AppSettings().Roles())
            SetDefault(slot, slot.Flow == AudioFlow.Playback ? "demo-headset" : "demo-headset-mic");
    }
    public void RefreshNotifications() { }
    public void Dispose() { }
}
