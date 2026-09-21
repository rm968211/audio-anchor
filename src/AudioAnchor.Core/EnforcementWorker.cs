using System.Threading.Channels;

namespace AudioAnchor.Core;

public sealed record WorkerTiming(TimeSpan HealthInterval, TimeSpan MinimumInterval, TimeSpan RetryDelay)
{
    public static WorkerTiming Default { get; } = new(TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(350));
}

public sealed class EnforcementWorker : IAsyncDisposable
{
    private readonly IAudioBackend _backend;
    private readonly WorkerTiming _timing;
    private readonly object _gate = new();
    private AppSettings _settings;
    private readonly CancellationTokenSource _stop = new();
    private readonly Channel<bool> _requests = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
    private readonly Task _worker;
    private readonly Task _health;
    private int _refreshRequested = 1;
    private int _disposed;
    public event Action<EnforcementReport>? Updated;

    public EnforcementWorker(IAudioBackend backend, AppSettings settings, WorkerTiming? timing = null)
    {
        _backend = backend;
        _settings = settings;
        _timing = timing ?? WorkerTiming.Default;
        _backend.Changed += Request;
        _worker = Task.Run(RunAsync);
        _health = Task.Run(HealthAsync);
        Request();
    }

    public void Configure(AppSettings settings)
    {
        lock (_gate) { _settings = settings; }
        Request();
    }

    public void Request() => _requests.Writer.TryWrite(true);
    public void Refresh()
    {
        Interlocked.Exchange(ref _refreshRequested, 1);
        Request();
    }

    private async Task HealthAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(_timing.HealthInterval);
            while (await timer.WaitForNextTickAsync(_stop.Token)) Refresh();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private async Task RunAsync()
    {
        try
        {
            while (await _requests.Reader.WaitToReadAsync(_stop.Token))
            {
                while (_requests.Reader.TryRead(out _)) { }
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    EnforcementReport report;
                    try
                    {
                        if (Interlocked.Exchange(ref _refreshRequested, 0) == 1) _backend.RefreshNotifications();
                        lock (_gate) { report = EnforcementPolicy.Reconcile(_backend, _settings); }
                    }
                    catch (Exception ex) { report = new([], 0, $"Audio connection unavailable: {ex.Message}"); }
                    Updated?.Invoke(report);
                    if (!report.HasErrors) break;
                    Interlocked.Exchange(ref _refreshRequested, 1);
                    await Task.Delay(_timing.RetryDelay * (attempt + 1), _stop.Token);
                }
                // Limits both failing notifications and utilities repeatedly fighting our defaults.
                await Task.Delay(_timing.MinimumInterval, _stop.Token);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _backend.Changed -= Request;
        await _stop.CancelAsync();
        _requests.Writer.TryComplete();
        await Task.WhenAll(_worker, _health);
        _stop.Dispose();
    }
}
