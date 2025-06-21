using VideoGenerator.Models;

namespace VideoGenerator.Services.Abstractions;

public interface IPythonExecutor
{
    Task ExecuteAsync(PythonGenerationRequest request, CancellationToken cancellationToken);
    event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;
}

public interface IFileManager
{
    string GenerateOutputPath(string? outputDirectory);
}

public interface IModelManager
{
    Task<bool> IsLoadedAsync();
    Task<bool> LoadAsync(string modelPath);
    string GetModelPath();
}


