using Microsoft.UI.Xaml.Controls;
using TimeTracker.ViewModels;

namespace TimeTracker.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }
        public SettingsPage()
        {
            InitializeComponent();
            ViewModel = new SettingsViewModel(App.SettingsService);
            DataContext = ViewModel;
        }
    }
}
