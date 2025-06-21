public interface IVideoGenerationService
{
    Task<VideoGenerationResult> GenerateVideoAsync(VideoGenerationRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsModelLoadedAsync();
    Task<bool> LoadModelAsync(string modelPath);
    event EventHandler<VideoGenerationProgressEventArgs>? ProgressChanged;
}
