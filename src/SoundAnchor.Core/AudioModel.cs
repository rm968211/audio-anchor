namespace SoundAnchor.Core;

public enum AudioFlow { Playback, Recording }
public enum AudioRole { Console, Multimedia, Communications }
public readonly record struct AudioSlot(AudioFlow Flow, AudioRole Role);
public sealed record AudioDevice(string Id, string Name, AudioFlow Flow, bool Available = true);
public sealed record DevicePreference(string Id, string Name);

public sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;
    public bool Paused { get; init; }
    public DevicePreference? Playback { get; init; }
    public DevicePreference? CommunicationsPlayback { get; init; }
    public DevicePreference? Recording { get; init; }
    public DevicePreference? CommunicationsRecording { get; init; }

    public IEnumerable<(AudioSlot Slot, DevicePreference? Preference)> Roles()
    {
        yield return (new(AudioFlow.Playback, AudioRole.Console), Playback);
        yield return (new(AudioFlow.Playback, AudioRole.Multimedia), Playback);
        yield return (new(AudioFlow.Playback, AudioRole.Communications), CommunicationsPlayback);
        yield return (new(AudioFlow.Recording, AudioRole.Console), Recording);
        yield return (new(AudioFlow.Recording, AudioRole.Multimedia), Recording);
        yield return (new(AudioFlow.Recording, AudioRole.Communications), CommunicationsRecording);
    }
}

public interface IAudioBackend : IDisposable
{
    event Action? Changed;
    IReadOnlyList<AudioDevice> GetDevices();
    string? GetDefault(AudioSlot slot);
    void SetDefault(AudioSlot slot, string deviceId);
    void RefreshNotifications();
}

public enum SlotState { Unmanaged, Paused, Protected, Waiting, Error }
public sealed record SlotStatus(AudioSlot Slot, string? PreferredId, string? CurrentId, SlotState State, string? Error = null);
public sealed record EnforcementReport(IReadOnlyList<SlotStatus> Slots, int Corrections, string? Error = null)
{
    public bool HasErrors => Error is not null || Slots.Any(s => s.State == SlotState.Error);
    public string Summary => Error is not null ? Error : HasErrors ? "Could not restore one or more devices. See details below."
        : Slots.Any(s => s.State == SlotState.Paused) ? "Paused — Windows can change your defaults."
        : Slots.Any(s => s.State == SlotState.Waiting) ? "Waiting for a preferred device to reconnect."
        : Slots.Any(s => s.State == SlotState.Protected) ? "Your preferred audio devices are protected."
        : "Choose your preferred devices, then select Save and apply.";
}
