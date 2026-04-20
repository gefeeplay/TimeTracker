using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TimeTracker.Models;
using TimeTracker.Monitoring;
using TimeTracker.Services;

namespace TimeTracker.ViewModels;

public partial class DashboardViewModel : INotifyPropertyChanged
{
    private readonly ActivityTracker _activityTracker;
    private readonly UsageService _usageService;
    private readonly StatisticsService _statsService;

    private readonly DispatcherQueue _dispatcher;

    private const string NO_DATA = "Нет данных";

    private string _totalTodayTitle = "ВСЕГО СЕГОДНЯ";
    private string _totalTodayTime = "0м";
    private string _totalTodayDelta = NO_DATA;

    private string _mostFrequentTitle = "САМОЕ ЧАСТОЕ";
    private string _mostFrequentCategory = NO_DATA;
    private string _mostFrequentDescription = "Начните использовать приложение";

    private string _windowSwitchesTitle = "СМЕНА ОКОН";
    private string _windowSwitchesCount = "0";
    private string _windowSwitchesDescription = NO_DATA;

    private string _weekActivityTitle = "Активность за неделю";

    private string _dailyGoalTitle = "Дневная цель";
    private string _dailyGoalDescription = NO_DATA;
    private string _dailyGoalPercent = "0%";
    private double _dailyGoalPercentValue = 0;

    private string _tipsTitle = "Умные советы";
    private string _tipsText = "Начните отслеживать активность";

    // Константа дневной цели в секундах (4 часов)
    private const int DAILY_GOAL_SECONDS = 4 * 60 * 60;

    public DashboardViewModel(ActivityTracker activityTracker, UsageService usageService, StatisticsService statisticsService, DispatcherQueue dispatcher)
    {

        _activityTracker = activityTracker;
        _usageService = usageService;
        _dispatcher = dispatcher;
        _statsService = statisticsService;

        _activityTracker.OnStatsUpdated += HandleStatsUpdated;

        WeekActivitySeries = Array.Empty<ISeries>();
        WeekActivityXAxes = Array.Empty<Axis>();

        LoadData(); // начальная загрузка    
    }

    private void HandleStatsUpdated()
    {
        //важно: UI поток!
        _dispatcher.TryEnqueue(() =>
        {
            LoadData();
        });
    }

    public void Dispose()
    {
        _activityTracker.OnStatsUpdated -= HandleStatsUpdated;
    }

    public void Initialize()
    {
        //var appId = App.UsageService.GetOrCreateApplication("test_app");

        //App.UsageService.AddSession(new UsageSession
        //{
        //    ApplicationId = appId, // теперь корректно
        //    StartTime = DateTime.Now.AddMinutes(-30),
        //    EndTime = DateTime.Now,
        //    DurationSeconds = 1800,
        //    CreatedAt = DateTime.Now
        //});

        App.StatisticsService.RecalculateDailyStats(DateTime.Today);

        LoadData();
    }

    private void LoadData()
    {
        var today = DateTime.Today.Date;
        System.Diagnostics.Debug.WriteLine("сегодня: " + today);
        var weekStart = today.AddDays(-6);

        // Общее время сегодня
        var totalSecondsToday = _statsService.GetTotalTimeForDate(today);
        System.Diagnostics.Debug.WriteLine("Всего секунд сегодня: " + totalSecondsToday);
        var previousTotalSeconds = _statsService.GetTotalTimeForDate(today.AddDays(-1));

        // если нет данных — просто показываем 0
        if (totalSecondsToday == 0)
        {
            TotalTodayTime = "0м";
            TotalTodayDelta = "Нет данных";

            MostFrequentCategory = "Нет данных";
            MostFrequentDescription = "Начните использовать приложение";

            WindowSwitchesCount = "0";
            WindowSwitchesDescription = "Нет данных";

            Applications.Clear();

            TipsText = "Начните отслеживать активность";

            return; // ВАЖНО
        }


        TotalTodayTime = FormatTime(totalSecondsToday);
        
        // Вычисление процента изменения
        if (previousTotalSeconds > 0)
        {
            var delta = ((double)(totalSecondsToday - previousTotalSeconds) / previousTotalSeconds) * 100;
            TotalTodayDelta = delta >= 0 ? $"+{delta:F0}%" : $"{delta:F0}%";
        }
        else
        {
            TotalTodayDelta = totalSecondsToday > 0 ? "Новый день" : "Нет данных";
        }

        // Самое частое приложение
        var mostFrequent = _statsService.GetMostFrequentApp(today, today.AddDays(1));
        if (mostFrequent.HasValue)
        {
            MostFrequentCategory = mostFrequent.Value.AppName;
            
            var totalSeconds = _statsService.GetTotalTimeForDate(today);
            if (totalSeconds > 0)
            {
                var percent = (double)totalSecondsToday / totalSeconds * 100;
                MostFrequentDescription = $"{percent:F0}% от общего времени";
            }
            else
            {
                MostFrequentDescription = "Нет данных";
            }
        }
        else
        {
            MostFrequentCategory = "Нет данных";
            MostFrequentDescription = "Начните использовать приложение";
        }

        // Количество переключений окон (сессий)
        var switchesCount = _statsService.GetWindowSwitchesCount(today, today.AddDays(1));
        WindowSwitchesCount = switchesCount.ToString();
        WindowSwitchesDescription = switchesCount > 0 
            ? $"В среднем раз в {Math.Max(1, (8 * 3600) / Math.Max(1, switchesCount))} минут"
            : "Нет данных";

        // Данные для графика за неделю
        var weeklyData = _statsService.GetWeeklyActivity();
        var weekValues = new List<int>();
        var weekLabels = new List<string>();

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayData = weeklyData.FirstOrDefault(d => d.Date.Date == date.Date);
            weekValues.Add(dayData.TotalSeconds);
            weekLabels.Add(GetDayName(date.DayOfWeek));
        }

        // Обновление серии графика
        WeekActivitySeries = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = weekValues,
                Fill = new SolidColorPaint(SKColor.Parse("#E3F2FD")),
                Stroke = new SolidColorPaint(SKColor.Parse("#2196F3")) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(SKColor.Parse("#2196F3")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#2196F3")) { StrokeThickness = 2 },
                GeometrySize = 10,
                LineSmoothness = 0.3
            }
        };

        WeekActivityXAxes = new Axis[]
        {
            new Axis
            {
                Labels = weekLabels,
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB")) { StrokeThickness = 1 }
            }
        };

        // Приложения за сегодня
        var apps = _statsService.GetAppsWithCategories(today, today.AddDays(1));
        Applications.Clear();
        foreach (var app in apps.Take(10))
        {
            Applications.Add(new ApplicationUsage(
                app.AppName,
                app.CategoryName,
                FormatTime(app.TotalSeconds),
                app.IconPath
            ));
        }

        // Дневная цель
        var goalPercent = Math.Min(100, (int)((double)totalSecondsToday / DAILY_GOAL_SECONDS * 100));
        var remainingSeconds = DAILY_GOAL_SECONDS - totalSecondsToday;
        
        DailyGoalPercentValue = goalPercent;
        DailyGoalPercent = $"{goalPercent}%";
        
        if (remainingSeconds > 0)
        {
            DailyGoalDescription = $"Осталось {FormatTime(remainingSeconds)} до лимита";
        }
        else
        {
            DailyGoalDescription = "Дневная цель достигнута!";
        }

        // Советы
        if (totalSecondsToday > DAILY_GOAL_SECONDS)
        {
            TipsText = "Вы превысили дневную цель. Постарайтесь сделать перерывы и отдохнуть.";
        }
        else if (apps.Any())
        {
            var topApp = apps.First();
            TipsText = $"Сегодня вы дольше всего использовали {topApp.AppName}. Попробуйте спланировать короткие перерывы для сохранения продуктивности.";
        }
        else
        {
            TipsText = "Начните отслеживать свое экранное время, просто работая за компьютером.";
        }
    }

    private static string FormatTime(int totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(totalSeconds);
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}ч {ts.Minutes}м";
        }
        return $"{ts.Minutes}м";
    }

    private static string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Пн",
            DayOfWeek.Tuesday => "Вт",
            DayOfWeek.Wednesday => "Ср",
            DayOfWeek.Thursday => "Чт",
            DayOfWeek.Friday => "Пт",
            DayOfWeek.Saturday => "Сб",
            DayOfWeek.Sunday => "Вс",
            _ => ""
        };
    }

    // Свойства для графиков
    public ISeries[] WeekActivitySeries { get; private set; }
    public Axis[] WeekActivityXAxes { get; private set; }

    public string TotalTodayTitle
    {
        get => _totalTodayTitle;
        set => SetField(ref _totalTodayTitle, value);
    }

    public string TotalTodayTime
    {
        get => _totalTodayTime;
        set => SetField(ref _totalTodayTime, value);
    }

    public string TotalTodayDelta
    {
        get => _totalTodayDelta;
        set => SetField(ref _totalTodayDelta, value);
    }

    public string MostFrequentTitle
    {
        get => _mostFrequentTitle;
        set => SetField(ref _mostFrequentTitle, value);
    }

    public string MostFrequentCategory
    {
        get => _mostFrequentCategory;
        set => SetField(ref _mostFrequentCategory, value);
    }

    public string MostFrequentDescription
    {
        get => _mostFrequentDescription;
        set => SetField(ref _mostFrequentDescription, value);
    }

    public string WindowSwitchesTitle
    {
        get => _windowSwitchesTitle;
        set => SetField(ref _windowSwitchesTitle, value);
    }

    public string WindowSwitchesCount
    {
        get => _windowSwitchesCount;
        set => SetField(ref _windowSwitchesCount, value);
    }

    public string WindowSwitchesDescription
    {
        get => _windowSwitchesDescription;
        set => SetField(ref _windowSwitchesDescription, value);
    }

    public string WeekActivityTitle
    {
        get => _weekActivityTitle;
        set => SetField(ref _weekActivityTitle, value);
    }

    public ObservableCollection<ApplicationUsage> Applications { get; set; }
    = new ObservableCollection<ApplicationUsage>();

    public string DailyGoalTitle
    {
        get => _dailyGoalTitle;
        set => SetField(ref _dailyGoalTitle, value);
    }

    public string DailyGoalDescription
    {
        get => _dailyGoalDescription;
        set => SetField(ref _dailyGoalDescription, value);
    }

    public string DailyGoalPercent
    {
        get => _dailyGoalPercent;
        set => SetField(ref _dailyGoalPercent, value);
    }

    public double DailyGoalPercentValue
    {
        get => _dailyGoalPercentValue;
        set => SetField(ref _dailyGoalPercentValue, value);
    }

    public string TipsTitle
    {
        get => _tipsTitle;
        set => SetField(ref _tipsTitle, value);
    }

    public string TipsText
    {
        get => _tipsText;
        set => SetField(ref _tipsText, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public record ApplicationUsage(string Name, string Category, string TimeText, string? IconPath);
