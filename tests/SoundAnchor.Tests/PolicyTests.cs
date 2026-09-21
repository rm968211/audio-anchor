using SoundAnchor.Core;

namespace SoundAnchor.Tests;

public class PolicyTests
{
    [Fact]
    public void FourSelectionsMapToAllSixRoles()
    {
        var mappings = TestAudio.All.Roles().ToDictionary(r => r.Slot, r => r.Preference?.Id);
        Assert.Equal(6, mappings.Count);
        Assert.Equal("speakers", mappings[new(AudioFlow.Playback, AudioRole.Console)]);
        Assert.Equal("speakers", mappings[new(AudioFlow.Playback, AudioRole.Multimedia)]);
        Assert.Equal("headset", mappings[new(AudioFlow.Playback, AudioRole.Communications)]);
        Assert.Equal("mic", mappings[new(AudioFlow.Recording, AudioRole.Console)]);
        Assert.Equal("mic", mappings[new(AudioFlow.Recording, AudioRole.Multimedia)]);
        Assert.Equal("headset-mic", mappings[new(AudioFlow.Recording, AudioRole.Communications)]);
    }
    [Fact]
    public void CorrectsAllRolesAndConvergesWithoutFurtherWrites()
    {
        using var audio = new TestAudio();
        var first = EnforcementPolicy.Reconcile(audio, TestAudio.All);
        Assert.Equal(6, first.Corrections);
        Assert.All(first.Slots, s => Assert.Equal(SlotState.Protected, s.State));
        Assert.Equal(0, EnforcementPolicy.Reconcile(audio, TestAudio.All).Corrections);
        Assert.Equal(6, audio.Writes);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PausedOrUnmanagedNeverWrites(bool paused)
    {
        using var audio = new TestAudio();
        var result = EnforcementPolicy.Reconcile(audio, paused ? TestAudio.All with { Paused = true } : new());
        Assert.Equal(0, audio.Writes);
        Assert.All(result.Slots, s => Assert.Equal(paused ? SlotState.Paused : SlotState.Unmanaged, s.State));
    }
    [Fact]
    public void MissingDeviceKeepsPreferenceAndDoesNotGuessDuplicateName()
    {
        using var audio = new TestAudio();
        audio.Available("speakers", false);
        var settings = new AppSettings { Playback = new("speakers", "Same name") };
        var report = EnforcementPolicy.Reconcile(audio, settings);
        Assert.Equal(2, report.Slots.Count(s => s.State == SlotState.Waiting));
        Assert.Equal(0, audio.Writes);
        audio.Available("speakers", true);
        Assert.Equal(2, EnforcementPolicy.Reconcile(audio, settings).Corrections);
    }
    [Fact]
    public void WrongDirectionCannotBecomeDefault()
    {
        using var audio = new TestAudio();
        var report = EnforcementPolicy.Reconcile(audio, new() { Recording = new("speakers", "Speakers") });
        Assert.Equal(2, report.Slots.Count(s => s.State == SlotState.Waiting));
        Assert.Equal(0, audio.Writes);
    }
    [Fact]
    public void SilentFailureIsDetectedByReadingBackTheDefault()
    {
        using var audio = new TestAudio { IgnoreWrites = true };
        var report = EnforcementPolicy.Reconcile(audio, TestAudio.All);
        Assert.True(report.HasErrors);
        Assert.Equal(0, report.Corrections);
        Assert.All(report.Slots, s => Assert.Equal(SlotState.Error, s.State));
    }
    [Fact]
    public void OneRoleFailureDoesNotBlockOtherRoles()
    {
        using var audio = new TestAudio { FailingSlot = new(AudioFlow.Playback, AudioRole.Console) };
        var report = EnforcementPolicy.Reconcile(audio, TestAudio.All);
        Assert.Equal(5, report.Corrections);
        Assert.Single(report.Slots, s => s.State == SlotState.Error);
    }
    [Fact]
    public void EnumerationFailureReturnsActionableError()
    {
        using var audio = new TestAudio { EnumerationFails = true };
        var report = EnforcementPolicy.Reconcile(audio, TestAudio.All);
        Assert.True(report.HasErrors);
        Assert.Contains("Audio service unavailable", report.Error);
        Assert.Equal(0, audio.Writes);
    }
}
