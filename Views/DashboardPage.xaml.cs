using Microsoft.UI.Xaml.Controls;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        
        ViewModel = new DashboardViewModel();
        this.DataContext = ViewModel;

        ViewModel.Initialize();
        ViewModel.StartAutoUpdate();
    }
}
