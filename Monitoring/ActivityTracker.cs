using System;
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

    private string _currentProcess = string.Empty;
    private string _iconPath = string.Empty;
    private DateTime _sessionStart;

    // настройки
    private const int IntervalMs = 2000;          // частота проверки
    private const int MinSessionSeconds = 2;      // минимальная длительность сессии

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
        var (name, path) = GetActiveProcessName();

        _currentProcess = name;
        _iconPath = path ?? string.Empty;

        _sessionStart = DateTime.Now;

        Debug.WriteLine($"[Tracker] Start: {_currentProcess}");

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
            var (activeProcess, exePath) = GetActiveProcessName();

            // игнор пустых значений
            if (string.IsNullOrEmpty(activeProcess))
                return;

            // игнор тех же процессов
            if (activeProcess == _currentProcess)
                return;

            Debug.WriteLine($"[Tracker] Switch: {_currentProcess} → {activeProcess}");

            // сохранить предыдущую сессию
            SaveSession();

            // начать новую
            _currentProcess = activeProcess;
            _iconPath = exePath ?? string.Empty;
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
            if (string.IsNullOrEmpty(_currentProcess))
                return;

            var endTime = DateTime.Now;
            var duration = (int)(endTime - _sessionStart).TotalSeconds;

            // игнор коротких сессий
            if (duration < MinSessionSeconds)
                return;

            // игнор системных процессов (по желанию)
            if (IsIgnoredProcess(_currentProcess))
                return;

            int appId = _usageService.GetOrCreateApplication(
                _currentProcess,
                string.IsNullOrEmpty(_iconPath) ? null : _iconPath
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

            //Обновление есть в UsageSession.AddSession
            //_statisticsService.RecalculateDailyStats(DateTime.Today);

            OnStatsUpdated?.Invoke(); // уведомляем UI

            Debug.WriteLine($"[Tracker] Saved: {_currentProcess} ({duration}s)");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Tracker SAVE ERROR] " + ex.Message);
        }
    }

    private (string Name, string? Path) GetActiveProcessName()
    {
        IntPtr hwnd = GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
            return (string.Empty, null);

        GetWindowThreadProcessId(hwnd, out uint pid);

        try
        {
            var process = Process.GetProcessById((int)pid);

            return (process.ProcessName, process.MainModule?.FileName);
        }
        catch
        {
            return (string.Empty, null);
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

    #region Win32

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    #endregion
}