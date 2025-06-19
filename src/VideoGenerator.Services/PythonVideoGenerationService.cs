using Microsoft.Extensions.Logging;
using VideoGenerator.Core.Interfaces;
using VideoGenerator.Core.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace VideoGenerator.Services;

public class PythonVideoGenerationService : IVideoGenerationService, IDisposable
{
    private readonly ILogger<PythonVideoGenerationService> _logger;
    private bool _isModelLoaded = false;
    private string? _modelPath;
    private readonly object _lock = new object();

    public event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;

    public PythonVideoGenerationService(ILogger<PythonVideoGenerationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> IsModelLoadedAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_isModelLoaded);
        }
    }

    public Task<bool> LoadModelAsync(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));

        lock (_lock)
        {
            try
            {
                _logger.LogInformation("Loading model from path: {ModelPath}", modelPath);
                
                // Check if path exists (either file or directory)
                bool pathExists = File.Exists(modelPath) || Directory.Exists(modelPath);
                
                if (!pathExists)
                {
                    _logger.LogError("Model path not found: {ModelPath}", modelPath);
                    return Task.FromResult(false);
                }

                // Additional validation for directories
                if (Directory.Exists(modelPath))
                {
                    _logger.LogInformation("Model path is a directory: {ModelPath}", modelPath);
                    
                    // Check if it looks like a valid model directory
                    var modelIndexPath = Path.Combine(modelPath, "model_index.json");
                    if (!File.Exists(modelIndexPath))
                    {
                        _logger.LogWarning("Directory does not contain model_index.json, but will proceed: {ModelPath}", modelPath);
                    }
                    else
                    {
                        _logger.LogInformation("Found model_index.json - this appears to be a complete model directory");
                    }
                }
                else
                {
                    _logger.LogInformation("Model path is a file: {ModelPath}", modelPath);
                }

                _modelPath = modelPath;
                _isModelLoaded = true;
                
                _logger.LogInformation("Model loaded successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model");
                _isModelLoaded = false;
                return Task.FromResult(false);
            }
        }
    }

    public async Task<VideoGenerationResult> GenerateVideoAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!_isModelLoaded || string.IsNullOrEmpty(_modelPath))
        {
            return VideoGenerationResult.Failure("Model is not loaded. Please load the model first.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting video generation with prompt: {Prompt}", request.Prompt);
            
            // Create output directory if it doesn't exist
            var outputDir = string.IsNullOrEmpty(request.OutputDirectory) 
                ? Path.GetDirectoryName(_modelPath) ?? Environment.CurrentDirectory
                : request.OutputDirectory;
                
            Directory.CreateDirectory(outputDir);

            // Generate unique filename
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"generated_video_{timestamp}.mp4";
            var outputPath = Path.Combine(outputDir, fileName);

            // Simulate video generation (replace with actual Python interop)
            var result = await GenerateVideoViaPythonAsync(request, outputPath, cancellationToken);
            
            stopwatch.Stop();

            if (result.IsSuccess && File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                _logger.LogInformation("Video generation completed successfully. File size: {FileSize} bytes", fileInfo.Length);
                
                return VideoGenerationResult.Success(
                    outputPath, 
                    request.Prompt, 
                    stopwatch.Elapsed, 
                    fileInfo.Length);
            }
            else
            {
                return VideoGenerationResult.Failure(result.ErrorMessage ?? "Unknown error occurred during video generation");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Video generation was cancelled");
            return VideoGenerationResult.Failure("Video generation was cancelled");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during video generation");
            return VideoGenerationResult.Failure($"Error during video generation: {ex.Message}");
        }
    }

    private async Task<VideoGenerationResult> GenerateVideoViaPythonAsync(VideoGenerationRequest request, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            // Use the external Python script instead of embedded code
            var scriptPath = GetPythonScriptPath();
            if (!File.Exists(scriptPath))
            {
                return VideoGenerationResult.Failure($"Python script not found: {scriptPath}");
            }

            var requestJson = JsonConvert.SerializeObject(new
            {
                model_path = _modelPath,
                prompt = request.Prompt,
                output_path = outputPath,
                duration_seconds = request.DurationSeconds,
                steps = request.Steps,
                guidance_scale = request.GuidanceScale,
                seed = request.Seed,
                width = request.Width,
                height = request.Height,
                fps = request.Fps
            });

            var requestPath = Path.Combine(Path.GetTempPath(), $"generation_request_{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken);

            // Start Python process
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" \"{requestPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };

            OnProgressChanged(new VideoGenerationProgressEventArgs { CurrentStep = 0, TotalSteps = request.Steps, StatusMessage = "Starting generation..." });

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Monitor progress by parsing Python output
            var outputTask = MonitorPythonOutputAsync(process, request.Steps, cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            // Cleanup temp files
            try
            {
                File.Delete(requestPath);
            }
            catch { /* Ignore cleanup errors */ }

            if (process.ExitCode == 0)
            {
                OnProgressChanged(new VideoGenerationProgressEventArgs { CurrentStep = request.Steps, TotalSteps = request.Steps, StatusMessage = "Generation completed!" });
                return VideoGenerationResult.Success(outputPath, request.Prompt, TimeSpan.Zero, 0);
            }
            else
            {
                _logger.LogError("Python script failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
                return VideoGenerationResult.Failure($"Python script failed: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Python script");
            return VideoGenerationResult.Failure($"Failed to execute Python script: {ex.Message}");
        }
    }

    private string GetPythonScriptPath()
    {
        // Get the application's base directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        
        // Look for python folder relative to the application
        var pythonDir = Path.Combine(appDir, "python");
        if (!Directory.Exists(pythonDir))
        {
            // Fallback to looking in the src directory during development
            var srcDir = Directory.GetParent(appDir)?.Parent?.Parent?.Parent?.FullName;
            if (srcDir != null)
            {
                pythonDir = Path.Combine(srcDir, "src", "python");
            }
        }
        
        return Path.Combine(pythonDir, "ltx_video_generator.py");
    }

    private async Task<string> MonitorPythonOutputAsync(Process process, int totalSteps, CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var buffer = new char[1024];
        
        try
        {
            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line == null) break;
                
                output.AppendLine(line);
                
                // Parse progress from Python output
                if (line.Contains("Step") && line.Contains("of"))
                {
                    // Try to extract step information from output like "Step 10 of 50"
                    var parts = line.Split(' ');
                    for (int i = 0; i < parts.Length - 2; i++)
                    {
                        if (parts[i].Equals("Step", StringComparison.OrdinalIgnoreCase) && 
                            int.TryParse(parts[i + 1], out int currentStep) &&
                            parts[i + 2].Equals("of", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(parts[i + 3], out int total))
                        {
                            OnProgressChanged(new VideoGenerationProgressEventArgs 
                            { 
                                CurrentStep = currentStep, 
                                TotalSteps = total, 
                                StatusMessage = line.Trim() 
                            });
                            break;
                        }
                    }
                }
                else if (line.Contains("Generating video") || line.Contains("Loading model") || line.Contains("Saving video"))
                {
                    OnProgressChanged(new VideoGenerationProgressEventArgs 
                    { 
                        CurrentStep = 0, 
                        TotalSteps = totalSteps, 
                        StatusMessage = line.Trim() 
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error monitoring Python output");
        }
        
        return output.ToString();
    }

    protected virtual void OnProgressChanged(VideoGenerationProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    public void Dispose()
    {
        // Cleanup resources if needed
    }
} 