using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoGenerator.Services;
using VideoGenerator.UI.ViewModels;
using VideoGenerator.UI.Services;
using VideoGenerator.Services.Abstractions;

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
            services.AddSingleton<IModelManager, ModelManager>();
            services.AddSingleton<IFileManager, FileManager>();
            services.AddSingleton<IPythonExecutor, PythonExecutor>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileDialogService, FileDialogService>();
            services.AddSingleton<IVideoGenerationService, PythonVideoGenerationService>();
            services.AddTransient<MainWindowViewModel>();
        });
    }
}