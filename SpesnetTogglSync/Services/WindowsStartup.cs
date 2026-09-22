using Microsoft.Win32;

namespace SpesnetTogglSync.Services;

internal static class WindowsStartup
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SpesnetTogglSync";

    public static bool TrySetEnabled(bool enabled, out string? error)
    {
        error = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
            {
                error = "Could not open the Windows startup settings.";
                return false;
            }

            if (!enabled)
            {
                if (key.GetValue(ValueName) != null)
                {
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }

                return true;
            }

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                error = "Could not determine the application path for Windows startup.";
                return false;
            }

            key.SetValue(ValueName, $"\"{exePath}\" --tray");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
