using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VideoGenerator.Models;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services;

public class PythonExecutor(ILogger<PythonExecutor> logger) : IPythonExecutor, IDisposable
{
    private readonly ILogger<PythonExecutor> _logger = logger;
    public event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;

    public async Task ExecuteAsync(PythonGenerationRequest request, CancellationToken cancellationToken)
    {
        var scriptPath = GetPythonScriptPath();
        ValidateScriptExists(scriptPath);
        
        var requestPath = await CreateRequestFileAsync(request, cancellationToken);
        
        try
        {
            using var process = CreatePythonProcess(scriptPath, requestPath);
            await ExecuteProcessAsync(process, request.Steps, cancellationToken);
        }
        finally
        {
            CleanupRequestFile(requestPath);
        }
    }

    private string GetPythonScriptPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var pythonDir = Path.Combine(appDir, "python");
        
        Directory.CreateDirectory(pythonDir);

        return Path.Combine(pythonDir, "ltx_video_generator.py");
    }

    private void ValidateScriptExists(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Python script not found: {scriptPath}");
        }
    }

    private async Task<string> CreateRequestFileAsync(PythonGenerationRequest request, CancellationToken cancellationToken)
    {
        var requestJson = JsonConvert.SerializeObject(request, Formatting.Indented);
        var requestPath = Path.Combine(Path.GetTempPath(), $"generation_request_{Guid.NewGuid():N}.json");
        
        await File.WriteAllTextAsync(requestPath, requestJson, cancellationToken);
        return requestPath;
    }

    private Process CreatePythonProcess(string scriptPath, string requestPath)
    {
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

        return new Process { StartInfo = startInfo };
    }

    private async Task ExecuteProcessAsync(Process process, int totalSteps, CancellationToken cancellationToken)
    {
        ReportProgress(0, totalSteps, "Starting generation...");
        
        process.Start();
        
        var outputTask = MonitorOutputAsync(process, totalSteps, cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync(cancellationToken);
        
        var error = await errorTask;
        
        if (process.ExitCode != 0)
        {
            _logger.LogError("Python script failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, error);
            throw new InvalidOperationException($"Python script failed: {error}");
        }
        
        ReportProgress(totalSteps, totalSteps, "Generation completed!");
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
        if (TryParseStepProgress(line, out var currentStep, out var total))
        {
            ReportProgress(currentStep, total, line.Trim());
        }
        else if (IsStatusUpdate(line))
        {
            ReportProgress(0, totalSteps, line.Trim());
        }
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