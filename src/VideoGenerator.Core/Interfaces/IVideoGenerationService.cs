using VideoGenerator.Core.Models;

namespace VideoGenerator.Core.Interfaces;

public interface IVideoGenerationService
{
    Task<VideoGenerationResult> GenerateVideoAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsModelLoadedAsync();
    Task<bool> LoadModelAsync(string modelPath);
    event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;
}

public class VideoGenerationProgressEventArgs : EventArgs
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string? StatusMessage { get; set; }
    public double ProgressPercentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
} 