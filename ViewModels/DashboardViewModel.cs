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
    private bool _boolTodayDelta = false;

    private string _mostFrequentTitle = "САМОЕ ЧАСТОЕ";
    private string _mostFrequentCategory = NO_DATA;
    private string _mostFrequentDescription = "Начните использовать приложение";
    private string _mostFrequentIcon = "";
    private double _mostFrequentSeconds = 0;

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
        WeekActivityYAxes = Array.Empty<Axis>();

        AppWeekActivitySeries = Array.Empty<ISeries>();
        AppWeekActivityXAxes = Array.Empty<Axis>();
        AppWeekActivityYAxes = Array.Empty<Axis>();

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

            return;
        }
        TotalTodayTime = FormatTime(totalSecondsToday);

        // Вычисление процента изменения
        if (previousTotalSeconds > 0)
        {
            var delta = ((double)(totalSecondsToday - previousTotalSeconds) / previousTotalSeconds) * 100;
            TotalTodayDelta = delta >= 0 ? $"+{delta:F0}%" : $"{delta:F0}%";
            BoolTodayDelta = delta >= 0;
        }
        else
        {
            TotalTodayDelta = totalSecondsToday > 0 ? "Новый день" : "Нет данных";
        }

        // Самое частое приложение
        LoadMostFrequentApp(today);

        // Количество переключений окон (сессий)
        LoadWindowSwitches(today, totalSecondsToday);

       // Данные для графика активности
        LoadWeeklyActivityData();

        // Все приложения для второго графика
        var TotalApps = _statsService.GetAllApplications();

        TotalApplications.Clear();
        foreach (var app in TotalApps)
        {
            TotalApplications.Add(app);
        }

        SelectDefaultApplication();
        // fallback если не нашли или нет данных
        SelectedApplication ??= TotalApplications.FirstOrDefault();
        // Данные для графика конкретного приложения
        if (SelectedApplication != null)
        {
            LoadAppWeeklyData(SelectedApplication.AppName);
        }

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
        LoadDailyGoal(totalSecondsToday);

        // Советы
        LoadTips(totalSecondsToday, apps);
    }

    private void LoadWeeklyActivityData()
    {
        // Данные для графика за неделю
        var weeklyData = _statsService.GetWeeklyActivity();
        var weekValues = new List<int>();
        var weekLabels = new List<string>();
        var today = DateTime.Today;

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayData = weeklyData.FirstOrDefault(d => d.Date.Date == date.Date);
            weekValues.Add(dayData.TotalSeconds);
            weekLabels.Add(GetDayName(date.DayOfWeek));
        }

        // Обновление серии графика
        ConfigureChart(
         weekValues,
         weekLabels,
         out var series,
         out var xAxes,
         out var yAxes);

        WeekActivitySeries = series;
        WeekActivityXAxes = xAxes;
        WeekActivityYAxes = yAxes;

        OnPropertyChanged(nameof(WeekActivitySeries));
        OnPropertyChanged(nameof(WeekActivityXAxes));
        OnPropertyChanged(nameof(WeekActivityYAxes));
    }

    private void LoadAppWeeklyData(string appName)
    {
        var today = DateTime.Today;

        var weeklyData = _statsService.GetWeeklyActivityByApp(appName);

        var weekValues = new List<int>();
        var weekLabels = new List<string>();

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayData = weeklyData.FirstOrDefault(d => d.Date.Date == date.Date);

            weekValues.Add(dayData.TotalSeconds);
            weekLabels.Add(GetDayName(date.DayOfWeek));
        }

        ConfigureChart(
        weekValues,
        weekLabels,
        out var series,
        out var xAxes,
        out var yAxes);

        AppWeekActivitySeries = series;
        AppWeekActivityXAxes = xAxes;
        AppWeekActivityYAxes = yAxes;

        OnPropertyChanged(nameof(AppWeekActivitySeries));
        OnPropertyChanged(nameof(AppWeekActivityXAxes));
        OnPropertyChanged(nameof(AppWeekActivityYAxes));
    }

    // Создание стилей графика
    private void ConfigureChart(
    List<int> values,
    List<string> labels,
    out ISeries[] series,
    out Axis[] xAxes,
    out Axis[] yAxes)
    {
        series = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = values,
                YToolTipLabelFormatter = chartPoint =>
                    FullFormatTime((int)chartPoint.Coordinate.PrimaryValue),
                Fill = new SolidColorPaint(SKColor.Parse("#E3F2FD")),
                Stroke = new SolidColorPaint(SKColor.Parse("#2196F3")) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(SKColor.Parse("#2196F3")),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#2196F3")) { StrokeThickness = 2 },
                GeometrySize = 10,
                LineSmoothness = 0.3
            }
        };

        xAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB")) { StrokeThickness = 1 }
            }
        };

        yAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                Labeler = value => FullFormatTime(value),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#E5E7EB")) { StrokeThickness = 1 }
            }
        };
    }

    private void LoadMostFrequentApp(DateTime today)
    {
        var mostFrequent = _statsService.GetMostFrequentApp(
            today,
            today.AddDays(1));

        if (mostFrequent.HasValue)
        {
            MostFrequentCategory = mostFrequent.Value.AppName;
            MostFrequentSeconds = mostFrequent.Value.TotalSeconds;
            MostFrequentIcon = mostFrequent.Value.IconPath!;

            var totalSeconds = _statsService.GetTotalTimeForDate(today);
            if (totalSeconds > 0)
            {
                var percent = (double)MostFrequentSeconds / totalSeconds * 100;
                MostFrequentDescription =$"{percent:F0}% от общего времени";
            }
            else MostFrequentDescription = "Нет данных";
        }
        else
        {
            MostFrequentCategory = "Нет данных";
            MostFrequentDescription = "Начните использовать приложение";
        }
    }

    private void LoadWindowSwitches(
    DateTime today,
    int totalSecondsToday)
    {
        var switchesCount = _statsService.GetWindowSwitchesCount(
            today,
            today.AddDays(1));

        WindowSwitchesCount = switchesCount.ToString();

        int averageSeconds = switchesCount > 0
            ? Math.Max(1, totalSecondsToday / switchesCount)
            : 0;

        string timeText;

        if (averageSeconds >= 3600)
            timeText = $"{averageSeconds / 3600} часа {averageSeconds / 60} минут";
        
        else if (averageSeconds >= 60)
            timeText = $"{averageSeconds / 60} минут";
        
        else timeText = $"{averageSeconds} секунд";

        WindowSwitchesDescription = switchesCount > 0
            ? $"В среднем раз в {timeText}"
            : "Нет данных";
    }

    private void SelectDefaultApplication()
    {
        var today = DateTime.Today;

        var mostFrequent = _statsService.GetMostFrequentApp(
            today,
            today.AddDays(1));

        if (mostFrequent.HasValue)
        {
            SelectedApplication = TotalApplications
                .FirstOrDefault(a => a.AppName == mostFrequent.Value.AppName);
        }
        SelectedApplication ??= TotalApplications.FirstOrDefault();
    }

    private void LoadDailyGoal(int totalSecondsToday)
    {
        var goalPercent = Math.Min(
            100,
            (int)((double)totalSecondsToday / DAILY_GOAL_SECONDS * 100));
        var remainingSeconds = DAILY_GOAL_SECONDS - totalSecondsToday;

        DailyGoalPercentValue = goalPercent;
        DailyGoalPercent = $"{goalPercent}%";

        if (remainingSeconds > 0)
            DailyGoalDescription = $"Осталось {FormatTime(remainingSeconds)} до лимита";
        else
            DailyGoalDescription = "Дневная цель достигнута!";
    }

    private void LoadTips(
    int totalSecondsToday,
    IEnumerable<(string AppName, string CategoryName, int TotalSeconds, string? IconPath)> apps)
    {
        if (totalSecondsToday > DAILY_GOAL_SECONDS)
            TipsText = "Вы превысили дневную цель. Постарайтесь сделать перерывы и отдохнуть.";
        else if (apps.Any())
        {
            var topApp = apps.First();
            TipsText =
                $"Сегодня вы дольше всего использовали {topApp.AppName}. " +
                $"Попробуйте спланировать короткие перерывы для сохранения продуктивности.";
        }
        else
            TipsText = "Начните отслеживать свое экранное время, просто работая за компьютером.";
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

    private static string FullFormatTime(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}ч {ts.Minutes}м {ts.Seconds}c";
        }
        return $"{ts.Minutes}м {ts.Seconds}c";
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
    public Style DeltaTextStyle =>
            BoolTodayDelta
                ? (Style)Microsoft.UI.Xaml.Application.Current.Resources["PositiveDeltaTextStyle"]
                : (Style)Microsoft.UI.Xaml.Application.Current.Resources["NegativeDeltaTextStyle"];

    // Свойства для графиков
    public ISeries[] WeekActivitySeries { get; private set; }
    public Axis[] WeekActivityXAxes { get; private set; }
    public Axis[] WeekActivityYAxes { get; private set; }

    public ISeries[] AppWeekActivitySeries { get; set; }
    public Axis[] AppWeekActivityXAxes { get; set; }
    public Axis[] AppWeekActivityYAxes { get; set; }

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

    public bool BoolTodayDelta
    {
        get => _boolTodayDelta;
        set => SetField(ref _boolTodayDelta, value);
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

    public string MostFrequentIcon
    {
        get => _mostFrequentIcon;
        set => SetField(ref _mostFrequentIcon, value);
    }

    public double MostFrequentSeconds
    {
        get => _mostFrequentSeconds;
        set => SetField(ref _mostFrequentSeconds, value);
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

    private bool _isWeekChartVisible = true;
    public bool IsWeekChartVisible
    {
        get => _isWeekChartVisible;
        set => SetField(ref _isWeekChartVisible, value);
    }

    public ObservableCollection<ApplicationUsage> Applications { get; set; }
    = new ObservableCollection<ApplicationUsage>();

    public ObservableCollection<AppWithCategory> TotalApplications { get; } = new();

    private AppWithCategory? _selectedApplication;

    public AppWithCategory? SelectedApplication
    {
        get => _selectedApplication;
        set
        {
            if (SetField(ref _selectedApplication, value) && value != null)
            {
                LoadAppWeeklyData(value.AppName);
            }
        }
    }

    private bool _isAppChartVisible = true;
    public bool IsAppChartVisible
    {
        get => _isAppChartVisible;
        set => SetField(ref _isAppChartVisible, value);
    }

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
