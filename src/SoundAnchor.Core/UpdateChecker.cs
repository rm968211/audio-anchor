using System.Net.Http;
using System.Text.Json;

namespace SoundAnchor.Core;

public sealed record ReleaseInfo(Version Version, string Url);

public interface IUpdateSource
{
    Task<string> FetchLatestReleaseJsonAsync(CancellationToken cancellationToken);
}

public static class UpdateChecker
{
    // GitHub's /releases/latest endpoint already excludes drafts and prereleases, matching the
    // stable-only SemVer policy in version.props.
    public static bool TryParseLatestRelease(string json, out ReleaseInfo? release)
    {
        release = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("tag_name", out var tagProperty) || !root.TryGetProperty("html_url", out var urlProperty)) return false;
            var tag = tagProperty.GetString();
            var url = urlProperty.GetString();
            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(url)) return false;
            var numeric = tag.StartsWith('v') ? tag[1..] : tag;
            if (!Version.TryParse(numeric, out var version)) return false;
            release = new(version, url);
            return true;
        }
        catch (JsonException) { return false; }
    }

    public static async Task<ReleaseInfo?> CheckAsync(IUpdateSource source, Version current, CancellationToken cancellationToken)
    {
        string json;
        // Best-effort: offline, rate-limited, or unreachable GitHub must never surface as an error.
        try { json = await source.FetchLatestReleaseJsonAsync(cancellationToken); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return null; }
        return TryParseLatestRelease(json, out var release) && release!.Version > current ? release : null;
    }
}
