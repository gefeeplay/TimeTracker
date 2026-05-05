using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;
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

    private async void ToggleWeekChart_Click(object sender, RoutedEventArgs e)
    {
        var newState = !ViewModel.IsWeekChartVisible;

        await AnimateChartAsync(WeekChartContainer, newState);

        ViewModel.IsWeekChartVisible = newState;
    }

    private async void ToggleAppChart_Click(object sender, RoutedEventArgs e)
    {
        var newState = !ViewModel.IsAppChartVisible;

        await AnimateChartAsync(AppChartContainer, newState);

        ViewModel.IsAppChartVisible = newState;
    }

    private async Task AnimateChartAsync(FrameworkElement element, bool expand)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // При раскрытии — сначала показать
        if (expand)
        {
            element.Visibility = Visibility.Visible;
        }

        // Scale
        var scaleAnim = compositor.CreateScalarKeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1f, expand ? 1f : 0f);
        scaleAnim.Duration = TimeSpan.FromMilliseconds(250);

        // Opacity
        var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.InsertKeyFrame(1f, expand ? 1f : 0f);
        opacityAnim.Duration = TimeSpan.FromMilliseconds(200);

        // Центр трансформации (сверху вниз)
        visual.CenterPoint = new System.Numerics.Vector3(
            (float)element.ActualWidth / 2,
            0,
            0);

        visual.StartAnimation("Scale.Y", scaleAnim);
        visual.StartAnimation("Opacity", opacityAnim);

        // Ждём завершения анимации
        await Task.Delay(250);

        // При схлопывании — убираем из layout
        if (!expand)
        {
            element.Visibility = Visibility.Collapsed;
        }
    }


}
