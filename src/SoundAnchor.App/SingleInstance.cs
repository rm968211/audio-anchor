using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace SoundAnchor.App;

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _pipe;
    private readonly CancellationTokenSource _stop = new();
    private Task? _listener;
    public bool IsFirst { get; }
    public SingleInstance(string settingsDirectory)
    {
        var identity = Environment.UserName + ":" + System.Diagnostics.Process.GetCurrentProcess().SessionId + ":" + Path.GetFullPath(settingsDirectory).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..24];
        _pipe = "SoundAnchor-" + hash;
        _mutex = new Mutex(true, @"Local\" + _pipe, out var first);
        IsFirst = first;
    }
    public void Send(string command)
    {
        using var client = new NamedPipeClientStream(".", _pipe, PipeDirection.Out, PipeOptions.CurrentUserOnly);
        client.Connect(2000);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine(command);
    }
    public void Listen(Action<string> received)
    {
        _listener = Task.Run(async () =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    using var pipe = new NamedPipeServerStream(_pipe, PipeDirection.In, 1, PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(_stop.Token);
                    using var reader = new StreamReader(pipe);
                    // A stalled same-user client cannot block the listener indefinitely.
                    var line = await reader.ReadLineAsync(_stop.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(3), _stop.Token);
                    if (line is not null) received(line);
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                catch (Exception ex) when (ex is IOException or TimeoutException) { }
            }
        });
    }
    public void Dispose()
    {
        _stop.Cancel();
        _listener?.GetAwaiter().GetResult();
        _stop.Dispose();
        if (IsFirst) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
