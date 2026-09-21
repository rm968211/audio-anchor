using System.Net.Http;
using System.Net.Http.Headers;

namespace SoundAnchor.Core;

// GitHub's REST API rejects requests without a User-Agent header.
public sealed class GitHubReleaseSource(string owner, string repository) : IUpdateSource, IDisposable
{
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<string> FetchLatestReleaseJsonAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repository}/releases/latest");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("SoundAnchor-UpdateCheck", "1"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose() => _client.Dispose();
}
