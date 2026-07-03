using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using TimeTracker.Models;
using TimeTracker.Services;

namespace TimeTracker.Monitoring;

public class ActivityTracker
{
    private readonly UsageService _usageService;
    //private readonly StatisticsService _statisticsService;

    private readonly Timer _timer;

    private enum TrackerState
    {
        ActiveApp,
        Idle
    }

    private TrackerState _state = TrackerState.ActiveApp;
    private ActiveApplicationInfo? _currentApp;
    private DateTime _sessionStart;

    // настройки
    private const int IntervalMs = 2000;          // частота проверки
    private const int MinSessionSeconds = 2;      // минимальная длительность сессии
    private const int IdleThreshold = 60;         // порог бездействия (в секундах)

    public event Action? OnStatsUpdated;

    public ActivityTracker(UsageService usageService)
    {
        _usageService = usageService;

        _timer = new Timer(IntervalMs);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = true;
    }

    public void Start()
    {
       _currentApp = GetActiveProcessInfo();

        if (_currentApp == null)
            return;

        _state = TrackerState.ActiveApp;
        _sessionStart = DateTime.Now;

        Debug.WriteLine($"[Tracker] Start: {_currentApp?.ProcessName}");

        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();

        Debug.WriteLine("[Tracker] Stop");

        SaveSession(); // сохранить последнюю сессию
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        try
        {

            bool isIdle = IsUserIdle();

            // Пользователь бездействует
            if (isIdle)
            {
                if (_state == TrackerState.Idle)
                {
                    Debug.WriteLine("[Tracker] User still idling");
                    return;
                }
                    
                Debug.WriteLine("[Tracker] User became idle");

                SaveSession();

                _state = TrackerState.Idle;
                _sessionStart = DateTime.Now;

                return;
            }

            // Получаем активное приложение
            var app = GetActiveProcessInfo();

            if (app == null)
                return;

            // Пользователь вернулся к работе
            if (_state == TrackerState.Idle)
            {
                Debug.WriteLine("[Tracker] User returned");

                SaveSession();

                _currentApp = app;
                _state = TrackerState.ActiveApp;
                _sessionStart = DateTime.Now;

                return;
            }

            // Проверка на смену активного приложения
            if (_currentApp == null)
                return;

            if (app.ProcessName == _currentApp.ProcessName)
                return;

            Debug.WriteLine($"[Tracker] Switch: {_currentApp.ProcessName} → {app.ProcessName}");


            // сохранить предыдущую сессию
            SaveSession();

            // начать новую
            _currentApp = app;
            _sessionStart = DateTime.Now;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Tracker ERROR] " + ex.Message);
        }
    }

    private void SaveSession()
    {
        try
        {
            var endTime = DateTime.Now;
            var duration = (int)(endTime - _sessionStart).TotalSeconds;

            // игнор коротких сессий
            if (duration < MinSessionSeconds)
                return;

            string processName;
            string displayName;
            string? iconPath;

            // бездействие
            if (_state == TrackerState.Idle)
            {
                processName = "__IDLE__";
                displayName = "Бездействие";
                iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "idle.png");
            }
            // обычный процесс
            else
            {
                if (_currentApp == null)
                    return;

                processName = _currentApp.ProcessName;
                displayName = _currentApp.DisplayName;
                iconPath = _currentApp.ExePath;

                // игнор системных процессов 
                if (IsIgnoredProcess(processName))
                    return;
            }

            int appId = _usageService.GetOrCreateApplication(
                processName,
                displayName,
                iconPath
            );

            var session = new UsageSession
            {
                ApplicationId = appId,
                StartTime = _sessionStart,
                EndTime = endTime,
                DurationSeconds = duration,
                CreatedAt = DateTime.Now
            };

            _usageService.AddSession(session);

            OnStatsUpdated?.Invoke(); // уведомляем UI

            Debug.WriteLine($"[Tracker] Saved: {displayName} ({duration}s)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Tracker SAVE ERROR] " + ex.Message);
        }
    }

    private ActiveApplicationInfo? GetActiveProcessInfo()
    {
        IntPtr hwnd = GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(hwnd, out uint pid);

        try
        {
            var process = Process.GetProcessById((int)pid);

            string processName = process.ProcessName;
            string? exePath = process.MainModule?.FileName;

            string displayName = processName;

            if (!string.IsNullOrEmpty(exePath))
            {
                var info = FileVersionInfo.GetVersionInfo(exePath);

                displayName =
                    info.FileDescription ??
                    info.ProductName ??
                    processName;
            }

            return new ActiveApplicationInfo(
                processName,
                displayName,
                exePath);
        }
        catch
        {
            return null;
        }
    }

    // фильтр ненужных процессов
    private bool IsIgnoredProcess(string processName)
    {
        string[] ignored =
        {
            "Idle",
            "System",
            "ApplicationFrameHost"
        };

        foreach (var item in ignored)
        {
            if (processName.Equals(item, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    // получение времени бездействия пользователя
    private TimeSpan GetIdleTime()
    {
        LASTINPUTINFO info = new()
        {
            cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>()
        };

        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        uint idle = unchecked((uint)Environment.TickCount) - info.dwTime;

        return TimeSpan.FromMilliseconds(idle);
    }

    // проверка бездействия пользователя
    private bool IsUserIdle()
    {
        return GetIdleTime().TotalSeconds >= IdleThreshold;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    #region Win32

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    #endregion
}