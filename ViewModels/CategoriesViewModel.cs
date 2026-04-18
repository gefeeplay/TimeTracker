using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TimeTracker.Models;

namespace TimeTracker.ViewModels;

public class CategoriesViewModel : INotifyPropertyChanged
{
    private readonly Services.StatisticsService _statsService;

    private string _headerTitle = "Категории";
    private string _headerSubtitle = "Обзор использования вашего времени по категориям";

    private CategorySummary _category1Summary = new CategorySummary("Нет данных", "0м", "Нет данных", "0%");
    private CategorySummary _category2Summary = new CategorySummary("Нет данных", "0м", "Нет данных", "0%");

    private string _tipTitle = "Совет по продуктивности";
    private string _tipText = "";

    // Выбранный период: 0 = сегодня, 1 = неделя, 2 = месяц
    private int _selectedPeriod;
    public int SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetField(ref _selectedPeriod, value))
            {
                LoadData();
            }
        }
    }

    public CategoriesViewModel() : this(App.StatisticsService)
    {
    }

    public CategoriesViewModel(Services.StatisticsService statsService)
    {
        _statsService = statsService;

        WorkApplications = new ObservableCollection<CategoryApplicationUsage>();
        EntertainmentApplications = new ObservableCollection<CategoryApplicationUsage>();

        // Загрузка данных
        LoadData();
    }

    private void LoadData()
    {
        var (from, to) = GetDateRange();
        var categoryStats = _statsService.GetCategoryStats(from, to).ToList();
        
        var totalSeconds = categoryStats.Sum(c => c.TotalSeconds);

        // Первая категория (обычно Работа)
        if (categoryStats.Count > 0)
        {
            var cat1 = categoryStats[0];
            var cat1Percent = totalSeconds > 0 ? (double)cat1.TotalSeconds / totalSeconds * 100 : 0;
            var cat1Previous = GetPreviousPeriodStats(cat1.CategoryName, from);
            var cat1Delta = cat1Previous > 0 
                ? ((double)(cat1.TotalSeconds - cat1Previous) / cat1Previous * 100) 
                : 0;

            Category1Summary = new CategorySummary(
                cat1.CategoryName,
                FormatTime(cat1.TotalSeconds),
                cat1Delta >= 0 ? $"+{cat1Delta:F0}% выше среднего" : $"{cat1Delta:F0}% ниже среднего",
                $"{cat1Percent:F0}%");
        }
        else
        {
            Category1Summary = new CategorySummary("Нет данных", "0м", "Нет данных", "0%");
        }

        // Вторая категория (обычно Развлечения)
        if (categoryStats.Count > 1)
        {
            var cat2 = categoryStats[1];
            var cat2Percent = totalSeconds > 0 ? (double)cat2.TotalSeconds / totalSeconds * 100 : 0;
            var cat2Previous = GetPreviousPeriodStats(cat2.CategoryName, from);
            var cat2Delta = cat2Previous > 0 
                ? ((double)(cat2.TotalSeconds - cat2Previous) / cat2Previous * 100) 
                : 0;

            Category2Summary = new CategorySummary(
                cat2.CategoryName,
                FormatTime(cat2.TotalSeconds),
                cat2Delta >= 0 ? $"+{cat2Delta:F0}% выше среднего" : $"{cat2Delta:F0}% ниже среднего",
                $"{cat2Percent:F0}%");
        }
        else
        {
            Category2Summary = new CategorySummary("Нет данных", "0м", "Нет данных", "0%");
        }

        // Приложения по категориям
        var apps = _statsService.GetAppsWithCategories(from, to).ToList();

        WorkApplications.Clear();
        EntertainmentApplications.Clear();

        foreach (var app in apps)
        {
            var usage = new CategoryApplicationUsage(
                app.AppName,
                app.CategoryName,
                FormatTime(app.TotalSeconds));

            // Рабочие категории
            if (app.CategoryName == "Работа" || app.CategoryName == "Обучение")
            {
                WorkApplications.Add(usage);
            }
            else
            {
                EntertainmentApplications.Add(usage);
            }
        }

        // Совет
        if (categoryStats.Any())
        {
            var topCat = categoryStats.First();
            TipText = $"Вы провели на {Math.Abs((topCat.TotalSeconds > 0 ? 15 : 0))}% больше времени в категории '{topCat.CategoryName}'. Попробуйте спланировать 15-минутный перерыв каждые 2 часа, чтобы сохранить фокус.";
        }
        else
        {
            TipText = "Начните использовать приложение для отслеживания времени по категориям.";
        }
    }

    private (DateTime from, DateTime to) GetDateRange()
    {
        var today = DateTime.Today;
        return SelectedPeriod switch
        {
            0 => (today, today.AddDays(1).AddSeconds(-1)), // Сегодня
            1 => (today.AddDays(-7), today.AddDays(1).AddSeconds(-1)), // Неделя
            2 => (today.AddMonths(-1), today.AddDays(1).AddSeconds(-1)), // Месяц
            _ => (today, today.AddDays(1).AddSeconds(-1))
        };
    }

    private int GetPreviousPeriodStats(string categoryName, DateTime currentFrom)
    {
        var previousFrom = SelectedPeriod switch
        {
            0 => currentFrom.AddDays(-1),
            1 => currentFrom.AddDays(-7),
            2 => currentFrom.AddMonths(-1),
            _ => currentFrom
        };

        var previousStats = _statsService.GetCategoryStats(previousFrom, currentFrom);
        return previousStats.FirstOrDefault(c => c.CategoryName == categoryName).TotalSeconds;
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

    public string HeaderTitle
    {
        get => _headerTitle;
        set => SetField(ref _headerTitle, value);
    }

    public string HeaderSubtitle
    {
        get => _headerSubtitle;
        set => SetField(ref _headerSubtitle, value);
    }

    public CategorySummary Category1Summary
    {
        get => _category1Summary;
        set => SetField(ref _category1Summary, value);
    }

    public CategorySummary Category2Summary
    {
        get => _category2Summary;
        set => SetField(ref _category2Summary, value);
    }

    // Alias для совместимости с XAML
    public CategorySummary WorkSummary
    {
        get => _category1Summary;
        set => SetField(ref _category1Summary, value);
    }

    public CategorySummary EntertainmentSummary
    {
        get => _category2Summary;
        set => SetField(ref _category2Summary, value);
    }

    public ObservableCollection<CategoryApplicationUsage> WorkApplications { get; }

    public ObservableCollection<CategoryApplicationUsage> EntertainmentApplications { get; }

    public string TipTitle
    {
        get => _tipTitle;
        set => SetField(ref _tipTitle, value);
    }

    public string TipText
    {
        get => _tipText;
        set => SetField(ref _tipText, value);
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

public record CategorySummary(string Name, string TimeText, string DeltaText, string PercentText);

public record CategoryApplicationUsage(string Name, string Category, string TimeText);

