using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TimeTracker.Models;
using static TimeTracker.ViewModels.CategoriesViewModel;

namespace TimeTracker.ViewModels
{

    public static class Periods
    {
        public const int Today = 0;
        public const int Week = 1;
        public const int Month = 2;
    }


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
        private int _selectedPeriod = Periods.Today;
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

            Category1Applications = new ObservableCollection<CategoryApplicationUsage>();
            Category2Applications = new ObservableCollection<CategoryApplicationUsage>();

            // Загрузка данных
            LoadData();
        }

        private void LoadData()
        {
            var (from, to) = GetDateRange();

            var categoryStats = _statsService
                .GetCategoryStats(from, to)
                .ToList();

            var apps = _statsService
                .GetAppsWithCategories(from, to)
                .ToList();

            var totalSeconds = categoryStats.Sum(c => c.TotalSeconds);

            Categories.Clear();

            // 1. Создаём категории
            foreach (var cat in categoryStats)
            {
                var percent = totalSeconds > 0
                    ? (double)cat.TotalSeconds / totalSeconds * 100
                    : 0;

                var previous = GetPreviousPeriodStats(cat.CategoryName, from);

                var delta = previous > 0
                    ? ((double)(cat.TotalSeconds - previous) / previous * 100)
                    : 0;

                var summary = new CategorySummary(
                    cat.CategoryName,
                    FormatTime(cat.TotalSeconds),
                    delta >= 0
                        ? $"+{delta:F0}% выше среднего"
                        : $"{delta:F0}% ниже среднего",
                    $"{percent:F0}%"
                );

                Categories.Add(new CategoryBlock
                {
                    Summary = summary
                });
            }

            // 2. Раскладываем приложения по категориям
            foreach (var app in apps)
            {
                var category = Categories
                    .FirstOrDefault(c => c.Summary?.Name == app.CategoryName);

                if (category == null)
                    continue;

                if (string.IsNullOrEmpty(app.CategoryName))
                    continue;

                category.Applications.Add(new CategoryApplicationUsage(
                    app.AppName,
                    app.CategoryName,
                    FormatTime(app.TotalSeconds),
                    app.IconPath
                ));
            }

            // 3. Совет (оставляем как есть, но лучше тоже переработать)
            if (categoryStats.Any())
            {
                var topCat = categoryStats.First();

                TipText =
                    $"Вы больше всего времени провели в категории '{topCat.CategoryName}'. " +
                    $"Попробуйте делать перерывы каждые 2 часа.";
            }
            else
            {
                TipText = "Начните отслеживание времени.";
            }
        }

        private (DateTime from, DateTime to) GetDateRange()
        {
            var today = DateTime.Today;
            return SelectedPeriod switch
            {
                Periods.Today => (today, today.AddDays(1).AddSeconds(-1)),
                Periods.Week => (today.AddDays(-7), today.AddDays(1).AddSeconds(-1)),
                Periods.Month => (today.AddMonths(-1), today.AddDays(1).AddSeconds(-1)),
                _ => (today, today.AddDays(1).AddSeconds(-1))
            };
        }

        private int GetPreviousPeriodStats(string categoryName, DateTime currentFrom)
        {
            var previousFrom = SelectedPeriod switch
            {
                Periods.Today => currentFrom.AddDays(-1),
                Periods.Week => currentFrom.AddDays(-7),
                Periods.Month => currentFrom.AddMonths(-1),
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
        
        public ObservableCollection<CategoryApplicationUsage> Category1Applications { get; }

        public ObservableCollection<CategoryApplicationUsage> Category2Applications { get; }

        public ObservableCollection<CategoryBlock> Categories { get; }
            = new ObservableCollection<CategoryBlock>();

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

    public class CategoryBlock : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;

        public CategoryBlock()
        {
            // Подписываемся на изменения коллекции, чтобы обновить зависимые свойства
            Applications.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(RemainingCount));
                OnPropertyChanged(nameof(TopApplications));
            };
        }

        public CategorySummary? Summary { get; set; }

        public ObservableCollection<CategoryApplicationUsage> Applications { get; set; }
            = new ObservableCollection<CategoryApplicationUsage>();

        public IEnumerable<CategoryApplicationUsage> TopApplications =>
            Applications.Take(5);

        public int RemainingCount =>
            Math.Max(0, Applications.Count - 5);

        public string GetFormattedText(int count) => $"И ещё {count} приложений";

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public record CategorySummary(string Name, string TimeText, string DeltaText, string PercentText);

    public record CategoryApplicationUsage(string Name, string Category, string TimeText, string? IconPath);
}
