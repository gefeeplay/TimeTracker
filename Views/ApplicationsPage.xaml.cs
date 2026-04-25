using Microsoft.UI.Xaml.Controls;
using TimeTracker.ViewModels;

namespace TimeTracker.Views
{
    public sealed partial class ApplicationsPage : Page
    {
        public ApplicationsViewModel ViewModel { get; }
        public ApplicationsPage()
        {
            InitializeComponent();
            ViewModel = new ApplicationsViewModel();
            DataContext = ViewModel;
        }
    }
}
