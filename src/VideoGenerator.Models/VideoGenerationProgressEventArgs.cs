namespace VideoGenerator.Models;

public class VideoGenerationProgressEventArgs : EventArgs
{
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string? StatusMessage { get; set; }
    public double ProgressPercentage => TotalSteps > 0 ? (double)CurrentStep / TotalSteps * 100 : 0;
} 