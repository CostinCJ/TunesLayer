using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TunesLayer.Core.Services;

public class StartupManager : IStartupManager
{
    private const string RunRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TunesLayer";

    public void SetStartupEnabled(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key == null)
            {
                Debug.WriteLine("Failed to open registry key for startup management.");
                return;
            }

            if (enable)
            {
                // Get the path to the executable
                var exePath = GetExecutablePath();
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    Debug.WriteLine($"Startup enabled. Path: {exePath}");
                }
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Debug.WriteLine("Startup disabled.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error setting startup: {ex.Message}");
        }
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: false);
            if (key == null)
            {
                return false;
            }

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking startup status: {ex.Message}");
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        // Try to get the entry assembly location first (works for published apps)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var location = entryAssembly.Location;
            if (!string.IsNullOrEmpty(location))
            {
                // If the location ends with .dll (happens in .NET Core/5+), replace with .exe
                if (location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    var exePath = Path.ChangeExtension(location, ".exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
                return location;
            }
        }

        // Fallback to process main module
        using var process = Process.GetCurrentProcess();
        var mainModule = process.MainModule;
        if (mainModule != null)
        {
            return mainModule.FileName;
        }

        return string.Empty;
    }
}
