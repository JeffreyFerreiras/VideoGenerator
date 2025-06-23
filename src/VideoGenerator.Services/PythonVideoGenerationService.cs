using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VideoGenerator.Models;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services;

// Simplified service that delegates to specialized components
public class PythonVideoGenerationService : IVideoGenerationService, IDisposable
{
    private readonly ILogger<PythonVideoGenerationService> _logger;
    private readonly IPythonExecutor _pythonExecutor;
    private readonly IFileManager _fileManager;
    private readonly IModelManager _modelManager;

    public event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;

    public PythonVideoGenerationService(
        ILogger<PythonVideoGenerationService> logger,
        IPythonExecutor pythonExecutor,
        IFileManager fileManager,
        IModelManager modelManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pythonExecutor = pythonExecutor ?? throw new ArgumentNullException(nameof(pythonExecutor));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
        
        _pythonExecutor.ProgressChanged += OnPythonExecutorProgressChanged;
    }

    public Task<bool> IsModelLoadedAsync() => _modelManager.IsLoadedAsync();

    public Task<bool> LoadModelAsync(string modelPath) => _modelManager.LoadAsync(modelPath);

    public async Task<VideoGenerationResult> GenerateVideoAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await _modelManager.IsLoadedAsync())
        {
            return VideoGenerationResult.Failure("Model is not loaded. Please load the model first.");
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var generationType = !string.IsNullOrEmpty(request.InputImagePath) ? "image-to-video" : "text-to-video";
            _logger.LogInformation("Starting {GenerationType} generation with prompt: {Prompt}", generationType, request.Prompt);
            
            var outputPath = _fileManager.GenerateOutputPath(request.OutputDirectory);
            var pythonRequest = CreatePythonRequest(request, outputPath);
            
            await _pythonExecutor.ExecuteAsync(pythonRequest, cancellationToken);
            
            stopwatch.Stop();
            return CreateSuccessResult(outputPath, request, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Video generation was cancelled");
            return VideoGenerationResult.Failure("Video generation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during video generation");
            return VideoGenerationResult.Failure($"Error during video generation: {ex.Message}");
        }
    }

    private PythonGenerationRequest CreatePythonRequest(VideoGenerationRequest request, string outputPath)
    {
        try
        {
            var modelPath = _modelManager.GetModelPath();
            _logger.LogDebug("Using model path: {ModelPath}", modelPath);
            
            var pythonRequest = new PythonGenerationRequest
            {
                ModelPath = modelPath,
                Prompt = request.Prompt,
                OutputPath = outputPath,
                DurationSeconds = request.DurationSeconds,
                Steps = request.Steps,
                GuidanceScale = request.GuidanceScale,
                Seed = request.Seed,
                Width = request.Width,
                Height = request.Height,
                Fps = request.Fps,
                InputImage = request.InputImagePath
            };
            
            var hasInputImage = !string.IsNullOrEmpty(request.InputImagePath);
            _logger.LogInformation("Created Python request - Model: {ModelPath}, Output: {OutputPath}, Prompt: {Prompt}, InputImage: {HasInputImage}", 
                modelPath, outputPath, request.Prompt, hasInputImage ? request.InputImagePath : "None");
            
            return pythonRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Python request. Model may not be loaded or path is invalid");
            throw;
        }
    }

    private VideoGenerationResult CreateSuccessResult(string outputPath, VideoGenerationRequest request, TimeSpan elapsed)
    {
        try
        {
            if (!File.Exists(outputPath))
            {
                _logger.LogError("Generated video file not found at expected path: {OutputPath}", outputPath);
                return VideoGenerationResult.Failure("Generated video file not found");
            }

            var fileInfo = new FileInfo(outputPath);
            _logger.LogInformation("Video generation completed successfully. File: {OutputPath}, Size: {FileSize} bytes, Duration: {Duration}", 
                outputPath, fileInfo.Length, elapsed);
            
            return VideoGenerationResult.Success(outputPath, request.Prompt, elapsed, fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating generated video file: {OutputPath}", outputPath);
            return VideoGenerationResult.Failure($"Error accessing generated video file: {ex.Message}");
        }
    }

    private void OnPythonExecutorProgressChanged(object? sender, VideoGenerationProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_pythonExecutor is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
