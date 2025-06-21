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
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (!await _modelManager.IsLoadedAsync()) 
            return VideoGenerationResult.Failure("Model is not loaded. Please load the model first.");

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting video generation with prompt: {Prompt}", request.Prompt);
            
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
        return new PythonGenerationRequest
        {
            ModelPath = _modelManager.GetModelPath(),
            Prompt = request.Prompt,
            OutputPath = outputPath,
            DurationSeconds = request.DurationSeconds,
            Steps = request.Steps,
            GuidanceScale = request.GuidanceScale,
            Seed = request.Seed,
            Width = request.Width,
            Height = request.Height,
            Fps = request.Fps
        };
    }

    private VideoGenerationResult CreateSuccessResult(string outputPath, VideoGenerationRequest request, TimeSpan elapsed)
    {
        if (!File.Exists(outputPath))
            return VideoGenerationResult.Failure("Generated video file not found");

        var fileInfo = new FileInfo(outputPath);
        _logger.LogInformation("Video generation completed successfully. File size: {FileSize} bytes", fileInfo.Length);
        
        return VideoGenerationResult.Success(outputPath, request.Prompt, elapsed, fileInfo.Length);
    }

    private void OnPythonExecutorProgressChanged(object? sender, VideoGenerationProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_pythonExecutor is IDisposable disposable)
            disposable.Dispose();
    }
}
