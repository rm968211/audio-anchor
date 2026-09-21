using Microsoft.Win32;

namespace AudioAnchor.App;

internal static class StartupRegistration
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue("AudioAnchor") is string; }
    }
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) key.SetValue("AudioAnchor", $"\"{Environment.ProcessPath}\" --background");
        else key.DeleteValue("AudioAnchor", false);
    }
}
