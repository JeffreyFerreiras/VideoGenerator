using Microsoft.Extensions.Logging;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services;

public class ModelManager : IModelManager
{
    private readonly ILogger<ModelManager> _logger;
    private readonly object _lock = new();
    private bool _isLoaded;
    private string? _modelPath;

    public ModelManager(ILogger<ModelManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> IsLoadedAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_isLoaded);
        }
    }

    public Task<bool> LoadAsync(string modelPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        lock (_lock)
        {
            try
            {
                _logger.LogInformation("Attempting to load model from: {ModelPath}", modelPath);
                
                if (!IsValidModelPath(modelPath))
                {
                    _logger.LogError("Model path validation failed: {ModelPath}", modelPath);
                    _isLoaded = false;
                    return Task.FromResult(false);
                }

                _modelPath = modelPath;
                _isLoaded = true;
                
                _logger.LogInformation("Model loaded successfully from: {ModelPath}", modelPath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while loading model from: {ModelPath}", modelPath);
                _isLoaded = false;
                return Task.FromResult(false);
            }
        }
    }

    public string GetModelPath()
    {
        lock (_lock)
        {
            return _modelPath ?? throw new InvalidOperationException("Model is not loaded. Please load a model first using the 'Load Model' button in the UI.");
        }
    }

    private bool IsValidModelPath(string modelPath)
    {
        var pathExists = File.Exists(modelPath) || Directory.Exists(modelPath);
        
        if (!pathExists)
        {
            _logger.LogError("Model path not found: {ModelPath}", modelPath);
            return false;
        }

        if (Directory.Exists(modelPath))
        {
            var modelIndexPath = Path.Combine(modelPath, "model_index.json");
            if (!File.Exists(modelIndexPath))
            {
                _logger.LogWarning("Directory does not contain model_index.json: {ModelPath}", modelPath);
            }
        }

        return true;
    }
}
