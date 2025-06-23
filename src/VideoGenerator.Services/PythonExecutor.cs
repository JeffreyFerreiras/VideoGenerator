using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VideoGenerator.Models;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services;

public class PythonExecutor(ILogger<PythonExecutor> logger) : IPythonExecutor, IDisposable
{
    private readonly ILogger<PythonExecutor> _logger = logger;
    public event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;

    public async Task ExecuteAsync(PythonGenerationRequest request, CancellationToken cancellationToken)
    {
        string? requestPath = null;
        
        try
        {
            _logger.LogInformation("Starting Python execution for model: {ModelPath}", request.ModelPath);
            
            var scriptPath = GetPythonScriptPath();
            ValidateScriptExists(scriptPath);
            
            requestPath = await CreateRequestFileAsync(request, cancellationToken);
            
            using var process = CreatePythonProcess(scriptPath, requestPath);
            await ExecuteProcessAsync(process, request.Steps, cancellationToken);
            
            _logger.LogInformation("Python execution completed successfully");
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Python directory not found. Error: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Python script or request file not found. Error: {ErrorMessage}", ex.Message);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Python execution was cancelled. Model: {ModelPath}", request.ModelPath);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Python script execution failed. Model: {ModelPath}, Error: {ErrorMessage}", 
                request.ModelPath, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Python execution. Model: {ModelPath}, Error: {ErrorMessage}", 
                request.ModelPath, ex.Message);
            throw;
        }
        finally
        {
            if (requestPath != null)
            {
                CleanupRequestFile(requestPath);
            }
        }
    }

    private string GetPythonScriptPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var pythonDir = Path.Combine(appDir, "python");
        if (!Directory.Exists(pythonDir))
        {
            _logger.LogError("Python directory not found: {PythonDirectory}", pythonDir);
            throw new DirectoryNotFoundException($"Python directory not found: {pythonDir}");
        }
        return Path.Combine(pythonDir, "ltx_video_generator.py");
    }

    private void ValidateScriptExists(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            _logger.LogError("Python script not found: {ScriptPath}", scriptPath);
            throw new FileNotFoundException($"Python script not found: {scriptPath}");
        }
    }

    private async Task<string> CreateRequestFileAsync(PythonGenerationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var requestJson = System.Text.Json.JsonSerializer.Serialize(request, options);
            var requestPath = Path.Combine(Path.GetTempPath(), $"generation_request_{Guid.NewGuid():N}.json");
            
            _logger.LogDebug("Creating request file: {RequestPath}", requestPath);
            _logger.LogDebug("Request JSON content: {RequestJson}", requestJson);
            
            await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken);
            
            _logger.LogInformation("Successfully created request file: {RequestPath}", requestPath);
            return requestPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create request file for model path: {ModelPath}", request.ModelPath);
            throw;
        }
    }

    private Process CreatePythonProcess(string scriptPath, string requestPath)
    {
        try
        {
            var workingDirectory = Path.GetDirectoryName(scriptPath);
            _logger.LogDebug("Creating Python process - Script: {ScriptPath}, Request: {RequestPath}, WorkingDir: {WorkingDirectory}", 
                scriptPath, requestPath, workingDirectory);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{scriptPath}\" \"{requestPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            var process = new Process { StartInfo = startInfo };
            _logger.LogInformation("Python process created successfully");
            return process;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Python process. Script: {ScriptPath}, Request: {RequestPath}, Error: {ErrorMessage}", 
                scriptPath, requestPath, ex.Message);
            throw;
        }
    }

    private async Task ExecuteProcessAsync(Process process, int totalSteps, CancellationToken cancellationToken)
    {
        try
        {
            ReportProgress(0, totalSteps, "Starting generation...");
            
            _logger.LogInformation("Starting Python process: {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
            
            if (!process.Start())
            {
                _logger.LogError("Failed to start Python process");
                throw new InvalidOperationException("Failed to start Python process");
            }
            
            _logger.LogDebug("Python process started with PID: {ProcessId}", process.Id);
            
            var outputTask = MonitorOutputAsync(process, totalSteps, cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync(cancellationToken);
            
            var error = await errorTask;
            
            _logger.LogInformation("Python process completed with exit code: {ExitCode}", process.ExitCode);
            
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Python stderr: {Error}", error);
            }
            
            if (process.ExitCode != 0)
            {
                _logger.LogError("Python script failed with exit code {ExitCode}. Stderr: {Error}", process.ExitCode, error);
                throw new InvalidOperationException($"Python script failed with exit code {process.ExitCode}: {error}");
            }
            
            ReportProgress(totalSteps, totalSteps, "Generation completed!");
            _logger.LogInformation("Python process execution completed successfully");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("Python process execution was cancelled");
            
            if (!process.HasExited)
            {
                try
                {
                    _logger.LogInformation("Attempting to kill Python process with PID: {ProcessId}", process.Id);
                    process.Kill();
                }
                catch (Exception killEx)
                {
                    _logger.LogWarning(killEx, "Failed to kill Python process: {ErrorMessage}", killEx.Message);
                }
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Python process execution. Exit code: {ExitCode}, Error: {ErrorMessage}", 
                process.HasExited ? process.ExitCode : -1, ex.Message);
            throw;
        }
    }

    private async Task MonitorOutputAsync(Process process, int totalSteps, CancellationToken cancellationToken)
    {
        try
        {
            while (!process.HasExited && !cancellationToken.IsCancellationRequested)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                // Log all Python output for debugging
                _logger.LogInformation("Python: {Output}", line);
                
                ParseProgressFromOutput(line, totalSteps);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error monitoring Python output");
        }
    }

    private void ParseProgressFromOutput(string line, int totalSteps)
    {
        // Parse PROGRESS: format from Python script
        if (TryParseProgressLine(line, out var currentStep, out var total))
        {
            var statusMessage = ExtractStatusFromRecentOutput(line);
            ReportProgress(currentStep, total, statusMessage ?? $"Step {currentStep} of {total}");
        }
        // Parse STATUS: format from Python script
        else if (TryParseStatusLine(line, out var status))
        {
            // Use the last known progress or estimate based on status
            var estimatedProgress = EstimateProgressFromStatus(status, totalSteps);
            ReportProgress(estimatedProgress.current, estimatedProgress.total, status);
        }
        // Legacy progress parsing for backward compatibility
        else if (TryParseStepProgress(line, out var legacyCurrentStep, out var legacyTotal))
        {
            ReportProgress(legacyCurrentStep, legacyTotal, line.Trim());
        }
        // Legacy status updates
        else if (IsStatusUpdate(line))
        {
            ReportProgress(0, totalSteps, line.Trim());
        }
    }

    private bool TryParseProgressLine(string line, out int currentStep, out int total)
    {
        currentStep = total = 0;
        
        // Look for "PROGRESS: Step X of Y" pattern
        if (line.StartsWith("PROGRESS:", StringComparison.OrdinalIgnoreCase))
        {
            var progressPart = line.Substring("PROGRESS:".Length).Trim();
            return TryParseStepProgress(progressPart, out currentStep, out total);
        }
        
        return false;
    }

    private bool TryParseStatusLine(string line, out string status)
    {
        status = string.Empty;
        
        // Look for "STATUS: message" pattern
        if (line.StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase))
        {
            status = line.Substring("STATUS:".Length).Trim();
            return !string.IsNullOrEmpty(status);
        }
        
        return false;
    }

    private string? _lastKnownStatus;

    private string? ExtractStatusFromRecentOutput(string line)
    {
        return _lastKnownStatus ?? "Processing...";
    }

    private (int current, int total) EstimateProgressFromStatus(string status, int totalSteps)
    {
        _lastKnownStatus = status;
        
        // Estimate progress based on status message content
        var lowerStatus = status.ToLowerInvariant();
        
        if (lowerStatus.Contains("loading model"))
        {
            return (1, totalSteps + 3);
        }
        else if (lowerStatus.Contains("model loaded") || lowerStatus.Contains("preparing"))
        {
            return (2, totalSteps + 3);
        }
        else if (lowerStatus.Contains("starting") && lowerStatus.Contains("generation"))
        {
            return (3, totalSteps + 3);
        }
        else if (lowerStatus.Contains("generating") || lowerStatus.Contains("denoising"))
        {
            // Try to extract step number from status
            var match = System.Text.RegularExpressions.Regex.Match(status, @"step (\d+)/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var step) && int.TryParse(match.Groups[2].Value, out var maxStep))
            {
                return (step + 3, totalSteps + 3); // +3 for the initial setup steps
            }
            return (totalSteps / 2 + 3, totalSteps + 3); // Rough middle estimate
        }
        else if (lowerStatus.Contains("saving"))
        {
            return (totalSteps + 3, totalSteps + 3);
        }
        else if (lowerStatus.Contains("completed"))
        {
            return (totalSteps + 3, totalSteps + 3);
        }
        
        // Default fallback
        return (0, totalSteps + 3);
    }

    private bool TryParseStepProgress(string line, out int currentStep, out int total)
    {
        currentStep = total = 0;
        
        if (!line.Contains("Step") || !line.Contains("of"))
        {
            return false;
        }

        var parts = line.Split(' ');
        for (int i = 0; i < parts.Length - 3; i++)
        {
            if (parts[i].Equals("Step", StringComparison.OrdinalIgnoreCase) && 
                int.TryParse(parts[i + 1], out currentStep) &&
                parts[i + 2].Equals("of", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parts[i + 3], out total))
            {
                return true;
            }
        }
        
        return false;
    }

    private bool IsStatusUpdate(string line)
    {
        return line.Contains("Generating video") || 
               line.Contains("Loading model") || 
               line.Contains("Saving video");
    }

    private void ReportProgress(int currentStep, int totalSteps, string statusMessage)
    {
        ProgressChanged?.Invoke(this, new VideoGenerationProgressEventArgs
        {
            CurrentStep = currentStep,
            TotalSteps = totalSteps,
            StatusMessage = statusMessage
        });
    }

    private void CleanupRequestFile(string requestPath)
    {
        try
        {
            File.Delete(requestPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup request file: {RequestPath}", requestPath);
        }
    }

    public void Dispose()
    {
        // No resources to dispose currently
    }
} 