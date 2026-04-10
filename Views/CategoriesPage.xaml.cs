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
    }
}
