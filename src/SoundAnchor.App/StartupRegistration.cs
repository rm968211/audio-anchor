using Microsoft.Win32;

namespace SoundAnchor.App;

internal static class StartupRegistration
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue("SoundAnchor") is string; }
    }
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) key.SetValue("SoundAnchor", $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue("SoundAnchor", false);
    }
}
