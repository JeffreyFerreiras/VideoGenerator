using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoGenerator.Services;
using VideoGenerator.UI.ViewModels;
using VideoGenerator.UI.Services;

namespace VideoGenerator.UI;

public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds UI services for VideoGenerator application to the host builder.
    /// </summary>
    public static IHostBuilder AddVideoGeneratorUI(this IHostBuilder builder)
    {
        return builder.ConfigureServices((context, services) =>
        {
            services.AddLogging();
            services.AddSingleton<Services.IDialogService, Services.DialogService>();
            services.AddSingleton<Services.IFileDialogService, Services.FileDialogService>();
            services.AddSingleton<IVideoGenerationService, PythonVideoGenerationService>();
            services.AddTransient<MainWindowViewModel>();
        });
    }
}