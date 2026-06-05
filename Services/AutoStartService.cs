using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TimeTracker.Data;

namespace TimeTracker.Services;

public class AutoStartService
{
    private const string RunKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private const string AppName = "TimeTracker";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);

        return key?.GetValue(AppName) != null;
    }

    public void Enable()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                writable: true);

        string exePath =
            Process.GetCurrentProcess().MainModule!.FileName!;

        key?.SetValue(AppName, $"\"{exePath}\" --autostart");
    }

    public void Disable()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKey,
                writable: true);

        key?.DeleteValue(AppName, false);
    }
}
