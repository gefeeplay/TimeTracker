using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTracker.ViewModels;

namespace TimeTracker.Views
{

    public sealed partial class CategoriesPage : Page
    {
        public CategoriesViewModel ViewModel { get; }

        public CategoriesPage()
        {
            InitializeComponent();
            ViewModel = new CategoriesViewModel();
            DataContext = ViewModel;
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedPeriod = 0;
        }

        private void Week_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedPeriod = 1;
        }

        private void Month_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedPeriod = 2;
        }
    }
}
