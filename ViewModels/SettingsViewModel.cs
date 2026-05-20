using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using TimeTracker.Services;

namespace TimeTracker.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;

    private int _dailyLimitHours;
    private string _apiKey = string.Empty;
    private bool _isAutoStartEnabled;

    public string HeaderTitle { get; } = "Настройки";

    public string HeaderSubtitle { get; } =
        "Управление параметрами приложения";

    public string TipsTitle { get; } = "Настройки приложения";

    public string TipsText { get; } =
        "Здесь можно настроить дневной лимит использования, " +
        "API ключ для AI-функций и автозапуск приложения.";

    public int DailyLimitHours
    {
        get => _dailyLimitHours;
        set
        {
            if (_dailyLimitHours != value)
            {
                _dailyLimitHours = value;
                OnPropertyChanged();
            }
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (_apiKey != value)
            {
                _apiKey = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsAutoStartEnabled
    {
        get => _isAutoStartEnabled;
        set
        {
            if (_isAutoStartEnabled != value)
            {
                _isAutoStartEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand LoadCommand { get; }

    public ICommand SaveCommand { get; }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadCommand = new RelayCommand(LoadSettings);
        SaveCommand = new RelayCommand(SaveSettings);

        LoadSettings();
    }

    private void LoadSettings()
    {
        DailyLimitHours =
            _settingsService.GetInt("DailyGoalHours", 4);

        ApiKey =
            _settingsService.Get("OpenRouterApiKey") ?? "";

        IsAutoStartEnabled =
            _settingsService.GetBool("AutoStartEnabled");
    }

    private void SaveSettings()
    {
        _settingsService.Set(
            "DailyGoalHours",
            DailyLimitHours.ToString());

        _settingsService.Set(
            "OpenRouterApiKey",
            ApiKey);

        _settingsService.Set(
            "AutoStartEnabled",
            IsAutoStartEnabled.ToString());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
