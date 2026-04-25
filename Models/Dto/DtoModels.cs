namespace TimeTracker.Models;

// Топ приложений
public class AppUsageDto
{
    public string AppName { get; set; } = "";
    public int TotalSeconds { get; set; }
}

// Приложение + категория
public class AppWithCategoryDto
{
    public string AppName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int TotalSeconds { get; set; }
    public string? IconPath { get; set; }
}

// Статистика категорий
public class CategoryStatDto
{
    public string CategoryName { get; set; } = "";
    public int TotalSeconds { get; set; }
    public int AppCount { get; set; }
}

// Данные для графика
public class DailyActivityDto
{
    public string Date { get; set; } = "";
    public int TotalSeconds { get; set; }
}

// Для смены категории
public class AppWithCategory
{
    public int Id { get; set; }

    public string AppName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string? IconPath { get; set; }
}