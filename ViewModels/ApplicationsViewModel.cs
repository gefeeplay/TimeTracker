using LiveChartsCore.Geo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TimeTracker.Models;
using TimeTracker.Services;

namespace TimeTracker.ViewModels {
    public class ApplicationItemViewModel : INotifyPropertyChanged
    {
        private int _categoryId;
        private bool _isChanged;

        public int Id { get; }
        public string Name { get; }

        public int CategoryId
        {
            get => _categoryId;
            set
            {
                if (_categoryId != value)
                {
                    _categoryId = value;
                    IsChanged = true;
                    OnPropertyChanged();
                }
            }
        }
        public string IconPath { get; }

        public bool IsChanged
        {
            get => _isChanged;
            set
            {
                _isChanged = value;
                OnPropertyChanged();
            }
        }

        public ApplicationItemViewModel(int id, string name, int categoryId, string iconPath)
        {
            Id = id;
            Name = name;
            CategoryId = categoryId;
            IconPath = iconPath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ApplicationsViewModel : INotifyPropertyChanged
    {
        private readonly StatisticsService _statsService;

        public string HeaderTitle { get; } = "Приложения";
        public string HeaderSubtitle { get; } = "Назначайте категории приложениям";

        public ObservableCollection<ApplicationItemViewModel> Applications { get; }
            = new();

        public ObservableCollection<Category> Categories { get; }
            = new();

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        public ApplicationsViewModel() : this(App.StatisticsService)
        {
        }

        public ApplicationsViewModel(StatisticsService statsService)
        {
            _statsService = statsService;

            LoadCommand = new RelayCommand(LoadData);
            SaveCommand = new RelayCommand(SaveChanges);

            LoadData();
        }

        private void LoadData()
        {
            Applications.Clear();
            Categories.Clear();

            var apps = _statsService.GetAllApplications();
            var categories = _statsService.GetAllCategories();

            foreach (var cat in categories)
                Categories.Add(cat);

            foreach (var app in apps)
            {
                Applications.Add(new ApplicationItemViewModel(
                    app.Id,
                    app.AppName,
                    app.CategoryId,
                    app.IconPath
                ));
            }
        }

        private void SaveChanges()
        {
            var changed = Applications.Where(a => a.IsChanged).ToList();

            foreach (var app in changed)
            {
                _statsService.UpdateApplicationCategory(app.Id, app.CategoryId);
                app.IsChanged = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
