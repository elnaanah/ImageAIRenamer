using System.Windows;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.DependencyInjection;
using ImageAIRenamer.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ImageAIRenamer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog
        Log.Logger = Infrastructure.Logging.LoggerExtensions.ConfigureLogger();

        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure services
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger);
            });
            services.AddApplicationServices(configuration);

            _serviceProvider = services.BuildServiceProvider();

            // Get logger for startup
            var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Application starting");

            // Set up main window with ViewModel
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
            
            var mainWindow = new MainWindow(mainViewModel, navigationService);
            MainWindow = mainWindow;
            mainWindow.Show();

            // Initialize navigation
            mainViewModel.Initialize();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"فشل بدء تشغيل التطبيق: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        _serviceProvider?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled exception occurred");
        MessageBox.Show($"حدث خطأ غير متوقع: {e.Exception.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
