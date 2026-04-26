using Microsoft.UI.Xaml;
using TimeTracker.ViewModels;

namespace TimeTracker.Views;

public sealed partial class MainWindow : Window
{
    public MainViewModel? ViewModel { get; private set; }

    public MainWindow()
    {
        InitializeComponent();

        RootGrid.Loaded += RootGrid_Loaded;

        this.AppWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            this.AppWindow.Hide();
        };

    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel = new MainViewModel(ContentFrame);

        RootGrid.DataContext = ViewModel;

        ContentFrame.Navigate(typeof(DashboardPage));
    }
}