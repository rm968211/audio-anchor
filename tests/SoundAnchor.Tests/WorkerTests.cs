using SoundAnchor.Core;

namespace SoundAnchor.Tests;

public class WorkerTests
{
    private static readonly WorkerTiming Fast = new(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(10));
    private static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate()) await Task.Delay(10, timeout.Token);
    }
    private static bool Correct(TestAudio audio, AppSettings settings) => settings.Roles().All(r => r.Preference is null || audio.GetDefault(r.Slot) == r.Preference.Id);

    [Fact, Trait("Category", "Integration")]
    public async Task NotificationRestoresAnExternalSwitchAndSelfEventsConverge()
    {
        using var audio = new TestAudio();
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast);
        await Until(() => Correct(audio, TestAudio.All));
        var slot = new AudioSlot(AudioFlow.Playback, AudioRole.Multimedia);
        audio.ExternalChange(slot, "headset");
        await Until(() => audio.GetDefault(slot) == "speakers");
        Assert.Equal(7, audio.Writes);
    }
    [Fact, Trait("Category", "Integration")]
    public async Task DisconnectReconnectionRestoresOriginalPreference()
    {
        using var audio = new TestAudio();
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast);
        await Until(() => Correct(audio, TestAudio.All));
        audio.Available("speakers", false);
        var slot = new AudioSlot(AudioFlow.Playback, AudioRole.Console);
        audio.ExternalChange(slot, "headset");
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Updated += r => { if (r.Slots.Any(s => s.State == SlotState.Waiting)) waiting.TrySetResult(); };
        worker.Request();
        await waiting.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("headset", audio.GetDefault(slot));
        audio.Available("speakers", true);
        await Until(() => Correct(audio, TestAudio.All));
    }
    [Fact, Trait("Category", "Integration")]
    public async Task PauseTakesEffectAndResumeReconciles()
    {
        using var audio = new TestAudio();
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast);
        await Until(() => Correct(audio, TestAudio.All));
        worker.Configure(TestAudio.All with { Paused = true });
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Updated += r => { if (r.Slots.All(s => s.State == SlotState.Paused)) paused.TrySetResult(); };
        audio.ExternalChange(new(AudioFlow.Playback, AudioRole.Console), "headset");
        worker.Request();
        await paused.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("headset", audio.GetDefault(new(AudioFlow.Playback, AudioRole.Console)));
        worker.Configure(TestAudio.All);
        await Until(() => Correct(audio, TestAudio.All));
    }
    [Fact, Trait("Category", "Integration")]
    public async Task RetryRecoversTransientFailure()
    {
        using var audio = new TestAudio { RemainingFailures = 2 };
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast);
        await Until(() => Correct(audio, TestAudio.All));
        Assert.Equal(8, audio.Writes);
    }
    [Fact, Trait("Category", "Integration")]
    public async Task PersistentFailureHasBoundedRetries()
    {
        using var audio = new TestAudio { RemainingFailures = 100 };
        await using var worker = new EnforcementWorker(audio, new() { CommunicationsPlayback = new("speakers", "Speakers") }, Fast);
        await Until(() => audio.Writes == 3);
        await worker.DisposeAsync();
        Assert.Equal(3, audio.Writes);
    }
    [Fact, Trait("Category", "Integration")]
    public async Task EventStormDoesNotProduceRedundantWrites()
    {
        using var audio = new TestAudio();
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast);
        Parallel.For(0, 10000, _ => audio.Notify());
        await Until(() => Correct(audio, TestAudio.All));
        Assert.Equal(6, audio.Writes);
    }
    [Fact, Trait("Category", "Integration")]
    public async Task HealthCheckRecoversWithoutADeviceNotification()
    {
        using var audio = new TestAudio { EnumerationFails = true };
        await using var worker = new EnforcementWorker(audio, TestAudio.All, Fast with { HealthInterval = TimeSpan.FromMilliseconds(80) });
        await Until(() => audio.Refreshes > 0);
        audio.EnumerationFails = false;
        await Until(() => Correct(audio, TestAudio.All));
    }
}
