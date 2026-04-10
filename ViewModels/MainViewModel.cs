using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TimeTracker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Frame _frame;

        private string _currentPage;
        public string CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel(Frame frame)
        {
            _frame = frame;

            CurrentPage = "DashboardPage";

            NavigateCommand = new RelayCommand(parameter =>
            {
                var pageName = parameter as string;
                if (string.IsNullOrWhiteSpace(pageName))
                    return;

                Type pageType = pageName switch
                {
                    "DashboardPage" => typeof(Views.DashboardPage),
                    "CategoriesPage" => typeof(Views.CategoriesPage),
                    _ => null
                };

                if (pageType is not null)
                {
                    _frame.Navigate(pageType);
                    CurrentPage = pageName; // ← ключевой момент
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
