using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TimeTracker.Views;

public sealed partial class TrayIconView : UserControl
{
    public ICommand ShowHideCommand { get; }
    public ICommand ExitCommand { get; }

    private bool _isVisible = true;

    public TrayIconView()
    {
        this.InitializeComponent();

        ShowHideCommand = new RelayCommand(_ => ToggleWindow());
        ExitCommand = new RelayCommand(_ => ExitApplication());
    }

    public void ToggleWindow()
    {
        var window = ((App)Application.Current).MainWindow;
        if (window == null)
        {
            return;
        }

        if (window.Visible)
        {
            window.Hide();
        }
        else
        {
            window.Show();
        }
        _isVisible = window.Visible;
    }

    public void ExitApplication()
    {
        var window = ((App)Application.Current).MainWindow;
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        window?.Close();
    }

}

