using Microsoft.UI.Xaml.Controls;
using System;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        
        ViewModel = new DashboardViewModel(App.ActivityTracker, App.UsageService, App.StatisticsService, this.DispatcherQueue);
        this.DataContext = ViewModel;

        this.Unloaded += OnUnloaded;
        //ViewModel.StartAutoUpdate();


    }
    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

}
