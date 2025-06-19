using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using VideoGenerator.Core.Interfaces;
using VideoGenerator.Services;
using VideoGenerator.UI.ViewModels;
using Serilog;

namespace VideoGenerator.UI;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Set up global exception handler
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<IVideoGenerationService, PythonVideoGenerationService>();
                    
                    // Register ViewModels
                    services.AddTransient<MainWindowViewModel>();
                })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    Directory.CreateDirectory(logDirectory);
                    
                    loggerConfiguration
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .WriteTo.File(Path.Combine(logDirectory, "VideoGenerator_.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                })
                .Build();

            var mainWindow = new MainWindow();
            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            mainWindow.SetViewModel(viewModel);
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}\n\nDetails: {ex}", 
                          "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nDetails: {e.Exception}", 
                      "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        MessageBox.Show($"A fatal error occurred: {exception?.Message ?? "Unknown error"}\n\nDetails: {exception}", 
                      "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
} 