namespace VideoGenerator.Core.Models;

public class VideoGenerationResult
{
    public bool IsSuccess { get; set; }
    public string? VideoFilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? Prompt { get; set; }
    public long FileSizeBytes { get; set; }

    public static VideoGenerationResult Success(string videoFilePath, string prompt, TimeSpan processingTime, long fileSizeBytes)
    {
        return new VideoGenerationResult
        {
            IsSuccess = true,
            VideoFilePath = videoFilePath,
            Prompt = prompt,
            ProcessingTime = processingTime,
            GeneratedAt = DateTime.Now,
            FileSizeBytes = fileSizeBytes
        };
    }

    public static VideoGenerationResult Failure(string errorMessage)
    {
        return new VideoGenerationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            GeneratedAt = DateTime.Now
        };
    }
} 