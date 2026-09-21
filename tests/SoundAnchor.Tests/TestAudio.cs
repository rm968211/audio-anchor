using SoundAnchor.Core;

namespace SoundAnchor.Tests;

internal sealed class TestAudio : IAudioBackend
{
    private readonly object _gate = new();
    private readonly Dictionary<AudioSlot, string?> _defaults = [];
    private List<AudioDevice> _devices =
    [new("speakers", "Same name", AudioFlow.Playback), new("headset", "Same name", AudioFlow.Playback),
     new("mic", "Microphone", AudioFlow.Recording), new("headset-mic", "Headset mic", AudioFlow.Recording)];
    public event Action? Changed;
    public int Writes;
    public int Refreshes;
    public int RemainingFailures;
    public bool IgnoreWrites;
    public bool EnumerationFails;
    public AudioSlot? FailingSlot;
    public List<(AudioSlot Slot, string Id)> History { get; } = [];
    public IReadOnlyList<AudioDevice> GetDevices() { lock (_gate) { if (EnumerationFails) throw new IOException("Audio service unavailable"); return _devices.ToArray(); } }
    public string? GetDefault(AudioSlot slot) { lock (_gate) return _defaults.GetValueOrDefault(slot); }
    public void SetDefault(AudioSlot slot, string id)
    {
        lock (_gate)
        {
            Interlocked.Increment(ref Writes);
            if (RemainingFailures-- > 0 || FailingSlot == slot) throw new IOException("Busy");
            History.Add((slot, id));
            if (IgnoreWrites) return;
            _defaults[slot] = id;
        }
        Changed?.Invoke();
    }
    public void ExternalChange(AudioSlot slot, string? id) { lock (_gate) _defaults[slot] = id; Changed?.Invoke(); }
    public void Available(string id, bool value) { lock (_gate) _devices = _devices.Select(d => d.Id == id ? d with { Available = value } : d).ToList(); Changed?.Invoke(); }
    public void Notify() => Changed?.Invoke();
    public void RefreshNotifications() => Interlocked.Increment(ref Refreshes);
    public void Dispose() { }
    public static AppSettings All { get; } = new()
    {
        Playback = new("speakers", "Speakers"), CommunicationsPlayback = new("headset", "Headset"),
        Recording = new("mic", "Microphone"), CommunicationsRecording = new("headset-mic", "Headset mic")
    };
}
