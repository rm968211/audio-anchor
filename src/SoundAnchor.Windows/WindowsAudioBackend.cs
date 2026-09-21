using System.Runtime.InteropServices;
using SoundAnchor.Core;

namespace SoundAnchor.Windows;

public sealed class WindowsAudioBackend : IAudioBackend
{
    private const string EnumeratorClass = "BCDE0395-E52F-467C-8E3D-C4579291692E";
    private readonly object _gate = new();
    private IDeviceEnumerator? _notifications;
    private readonly NotificationSink _sink;
    private bool _disposed;
    public event Action? Changed;

    public WindowsAudioBackend() => _sink = new(() => Changed?.Invoke());

    public IReadOnlyList<AudioDevice> GetDevices()
    {
        var result = new List<AudioDevice>();
        var enumerator = NativeMethods.Create<IDeviceEnumerator>(EnumeratorClass);
        try
        {
            foreach (var flow in Enum.GetValues<AudioFlow>())
            {
                NativeMethods.Check(enumerator.EnumAudioEndpoints((int)flow, 0xF, out var collection));
                try
                {
                    NativeMethods.Check(collection.GetCount(out var count));
                    for (uint i = 0; i < count; i++)
                    {
                        NativeMethods.Check(collection.Item(i, out var device));
                        try
                        {
                            NativeMethods.Check(device.GetId(out var id));
                            NativeMethods.Check(device.GetState(out var state));
                            var name = id;
                            if (device.OpenPropertyStore(0, out var store) >= 0)
                            {
                                try
                                {
                                    var key = new PropertyKey { Format = new("A45C254E-DF1C-4EFD-8020-67D146A850E0"), Id = 14 };
                                    // Disconnected/stale endpoints may have no readable friendly-name property.
                                    if (store.GetValue(ref key, out var value) >= 0)
                                    {
                                        try { if (value.Type == 31) name = Marshal.PtrToStringUni(value.Pointer) ?? id; }
                                        finally { NativeMethods.PropVariantClear(ref value); }
                                    }
                                }
                                finally { NativeMethods.Release(store); }
                            }
                            result.Add(new(id, name, flow, state == 1));
                        }
                        finally { NativeMethods.Release(device); }
                    }
                }
                finally { NativeMethods.Release(collection); }
            }
        }
        finally { NativeMethods.Release(enumerator); }
        return result;
    }

    public string? GetDefault(AudioSlot slot)
    {
        var enumerator = NativeMethods.Create<IDeviceEnumerator>(EnumeratorClass);
        try
        {
            var hr = enumerator.GetDefaultAudioEndpoint((int)slot.Flow, (int)slot.Role, out var device);
            if (hr == unchecked((int)0x80070490)) return null; // No endpoint for this role.
            NativeMethods.Check(hr);
            try { NativeMethods.Check(device.GetId(out var id)); return id; }
            finally { NativeMethods.Release(device); }
        }
        finally { NativeMethods.Release(enumerator); }
    }

    public void SetDefault(AudioSlot slot, string deviceId)
    {
        var policy = NativeMethods.Create<IPolicyConfig>("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
        try { NativeMethods.Check(policy.SetDefaultEndpoint(deviceId, (int)slot.Role)); }
        finally { NativeMethods.Release(policy); }
    }

    public void RefreshNotifications()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var replacement = NativeMethods.Create<IDeviceEnumerator>(EnumeratorClass);
            try { NativeMethods.Check(replacement.RegisterEndpointNotificationCallback(_sink)); }
            catch { NativeMethods.Release(replacement); throw; }
            var previous = _notifications;
            _notifications = replacement;
            if (previous is not null)
            {
                previous.UnregisterEndpointNotificationCallback(_sink);
                NativeMethods.Release(previous);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            if (_notifications is not null)
            {
                _notifications.UnregisterEndpointNotificationCallback(_sink);
                NativeMethods.Release(_notifications);
                _notifications = null;
            }
        }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public sealed class NotificationSink(Action signal) : INotificationClient
    {
        private int Signal() { try { signal(); } catch { /* Never throw through a COM callback. */ } return 0; }
        public int OnDeviceStateChanged(string id, uint state) => Signal();
        public int OnDeviceAdded(string id) => Signal();
        public int OnDeviceRemoved(string id) => Signal();
        public int OnDefaultDeviceChanged(int flow, int role, string? id) => Signal();
        public int OnPropertyValueChanged(string id, NotificationPropertyKey key) => Signal();
    }
}
