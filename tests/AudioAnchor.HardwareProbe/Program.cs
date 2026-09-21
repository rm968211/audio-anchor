using System.Text.Json;
using AudioAnchor.Core;
using AudioAnchor.Windows;

var exercise = args.Contains("--exercise-switching");
var verify = args.Contains("--verify-current");
using var audio = new WindowsAudioBackend();
audio.RefreshNotifications();
var devices = audio.GetDevices();
var snapshot = new AppSettings().Roles().Select(r => (r.Slot, Id: audio.GetDefault(r.Slot))).ToArray();
Console.WriteLine($"Native enumeration: {devices.Count} endpoints, {snapshot.Count(r => r.Id is not null)} defaults.");
if (!exercise && !verify) return 0;
var snapshotPath = Path.Combine(Path.GetTempPath(), $"AudioAnchor-audio-snapshot-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snapshot.Select(r => new { r.Slot.Flow, r.Slot.Role, r.Id }), new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Recovery snapshot: {snapshotPath}");
var tested = 0;
var failed = false;
EnforcementWorker? worker = null;
try
{
    foreach (var (slot, id) in snapshot)
    {
        if (id is null) { Console.WriteLine($"UNTESTED {slot}: no current device"); continue; }
        if (!exercise)
        {
            audio.SetDefault(slot, id);
            if (audio.GetDefault(slot) != id) throw new InvalidOperationException($"Could not verify {slot}");
        }
        else
        {
            var alternative = devices.FirstOrDefault(d => d.Flow == slot.Flow && d.Available && d.Id != id);
            if (alternative is null) { Console.WriteLine($"UNTESTED {slot}: no alternative endpoint"); continue; }
            // Limit the worker to this role group; other settings stay unmanaged.
            var preference = new DevicePreference(id, devices.FirstOrDefault(d => d.Id == id)?.Name ?? id);
            var settings = slot switch
            {
                { Flow: AudioFlow.Playback, Role: AudioRole.Communications } => new AppSettings { CommunicationsPlayback = preference },
                { Flow: AudioFlow.Playback } => new AppSettings { Playback = preference },
                { Role: AudioRole.Communications } => new AppSettings { CommunicationsRecording = preference },
                _ => new AppSettings { Recording = preference }
            };
            worker = new(audio, settings);
            await Task.Delay(250);
            audio.SetDefault(slot, alternative.Id);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (audio.GetDefault(slot) != id) await Task.Delay(20, timeout.Token);
            await worker.DisposeAsync();
            worker = null;
        }
        tested++;
        Console.WriteLine($"PASS {slot}");
    }
}
catch (Exception ex) { failed = true; Console.Error.WriteLine(ex); }
finally
{
    if (worker is not null) await worker.DisposeAsync();
    if (exercise)
        foreach (var (slot, id) in snapshot)
            if (id is not null)
                try { audio.SetDefault(slot, id); }
                catch (Exception ex) { failed = true; Console.Error.WriteLine($"RESTORE FAILED {slot}: {ex.Message}"); }
}
Console.WriteLine($"Verified {tested}/6 roles. Physical connection and sleep/resume tests are separate.");
return failed || tested == 0 ? 1 : 0;
