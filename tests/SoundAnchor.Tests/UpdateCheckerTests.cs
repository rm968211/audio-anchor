using System.Net.Http;
using SoundAnchor.Core;

namespace SoundAnchor.Tests;

public sealed class UpdateCheckerTests
{
    private sealed class FakeSource(string json) : IUpdateSource
    {
        public Task<string> FetchLatestReleaseJsonAsync(CancellationToken cancellationToken) => Task.FromResult(json);
    }
    private sealed class FailingSource(Exception exception) : IUpdateSource
    {
        public Task<string> FetchLatestReleaseJsonAsync(CancellationToken cancellationToken) => Task.FromException<string>(exception);
    }

    [Fact]
    public void ParsesTagAndStripsLeadingV()
    {
        Assert.True(UpdateChecker.TryParseLatestRelease("""{"tag_name":"v1.2.3","html_url":"https://example.test/1"}""", out var release));
        Assert.Equal(new Version(1, 2, 3), release!.Version);
        Assert.Equal("https://example.test/1", release.Url);
    }
    [Fact]
    public void ParsesTagWithoutLeadingV()
    {
        Assert.True(UpdateChecker.TryParseLatestRelease("""{"tag_name":"1.2.3","html_url":"https://example.test/1"}""", out var release));
        Assert.Equal(new Version(1, 2, 3), release!.Version);
    }
    [Theory]
    [InlineData("""{"html_url":"https://example.test/1"}""")]
    [InlineData("""{"tag_name":"v1.2.3"}""")]
    [InlineData("""{"tag_name":"","html_url":"https://example.test/1"}""")]
    [InlineData("""{"tag_name":"not-a-version","html_url":"https://example.test/1"}""")]
    [InlineData("not json")]
    public void RejectsMissingOrMalformedFields(string json) => Assert.False(UpdateChecker.TryParseLatestRelease(json, out _));

    [Fact]
    public async Task ReturnsReleaseWhenNewerThanCurrent()
    {
        var source = new FakeSource("""{"tag_name":"v0.4.0","html_url":"https://example.test/release"}""");
        var release = await UpdateChecker.CheckAsync(source, new Version(0, 3, 0), CancellationToken.None);
        Assert.NotNull(release);
        Assert.Equal(new Version(0, 4, 0), release!.Version);
    }
    [Fact]
    public async Task ReturnsNullWhenNotNewer()
    {
        var source = new FakeSource("""{"tag_name":"v0.3.0","html_url":"https://example.test/release"}""");
        Assert.Null(await UpdateChecker.CheckAsync(source, new Version(0, 3, 0), CancellationToken.None));
    }
    [Fact]
    public async Task ReturnsNullWhenOlder()
    {
        var source = new FakeSource("""{"tag_name":"v0.2.0","html_url":"https://example.test/release"}""");
        Assert.Null(await UpdateChecker.CheckAsync(source, new Version(0, 3, 0), CancellationToken.None));
    }
    [Theory]
    [MemberData(nameof(TransientFailures))]
    public async Task SwallowsTransientFailuresInsteadOfThrowing(Exception exception)
    {
        var source = new FailingSource(exception);
        Assert.Null(await UpdateChecker.CheckAsync(source, new Version(0, 3, 0), CancellationToken.None));
    }
    public static TheoryData<Exception> TransientFailures => new()
    {
        new HttpRequestException("offline"),
        new TaskCanceledException("timed out"),
        new System.Text.Json.JsonException("bad body"),
    };
}
