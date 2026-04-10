using LiveChartsCore;
using Microsoft.UI.Xaml;
using SQLitePCL;
using System;
using System.IO;
using TimeTracker.Data;
using TimeTracker.Monitoring;
using TimeTracker.Services;
using TimeTracker.Views;

namespace TimeTracker
{
    
    public partial class App : Application
    {
        private Window? _window;

        // Статический доступ к сервисам
        public static Database Database { get; private set; } = null!;
        public static UsageService UsageService { get; private set; } = null!;
        public static StatisticsService StatisticsService { get; private set; } = null!;
        public static CleanupService CleanupService { get; private set; } = null!;
        public static ActivityTracker ActivityTracker { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            Batteries.Init();

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "tracker.db");

            DbInitializer.Initialize(dbPath);

            System.Diagnostics.Debug.WriteLine(File.Exists(dbPath)
    ? "DB EXISTS"
    : "DB NOT FOUND");

            Database = new Database(dbPath);

            UsageService = new UsageService(Database);
            StatisticsService = new StatisticsService(Database);
            CleanupService = new CleanupService(Database);
            ActivityTracker = new ActivityTracker(UsageService);

            this.UnhandledException += (sender, e) =>
            {
                if (e.Exception is Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine("INNER: " + ex.InnerException.Message);
                    }
                }
            };

            System.Diagnostics.Debug.WriteLine("DB PATH: " + dbPath);

        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();

            ActivityTracker.Start();
        }
    }
}
