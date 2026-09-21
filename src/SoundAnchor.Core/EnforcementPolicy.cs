namespace SoundAnchor.Core;

public static class EnforcementPolicy
{
    public static EnforcementReport Reconcile(IAudioBackend backend, AppSettings settings)
    {
        var statuses = new List<SlotStatus>();
        var corrections = 0;
        IReadOnlyList<AudioDevice> devices;
        try { devices = backend.GetDevices(); }
        catch (Exception ex) { return new(statuses, 0, $"Audio devices unavailable: {ex.Message}"); }

        foreach (var (slot, preference) in settings.Roles())
        {
            string? current = null;
            try
            {
                current = backend.GetDefault(slot);
                var state = preference is null ? SlotState.Unmanaged
                    : settings.Paused ? SlotState.Paused
                    : !devices.Any(d => d.Id == preference.Id && d.Flow == slot.Flow && d.Available) ? SlotState.Waiting
                    : SlotState.Protected;
                if (state == SlotState.Protected && current != preference!.Id)
                {
                    backend.SetDefault(slot, preference.Id);
                    current = backend.GetDefault(slot);
                    if (current != preference.Id)
                        throw new InvalidOperationException("Windows did not retain the preferred device. Another audio utility may be changing it.");
                    corrections++;
                }
                statuses.Add(new(slot, preference?.Id, current, state));
            }
            catch (Exception ex) { statuses.Add(new(slot, preference?.Id, current, SlotState.Error, ex.Message)); }
        }
        return new(statuses, corrections);
    }
}
