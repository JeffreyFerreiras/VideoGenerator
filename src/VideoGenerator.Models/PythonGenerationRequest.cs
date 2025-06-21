namespace VideoGenerator.Models;

public class PythonGenerationRequest
{
    public string ModelPath { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int Steps { get; set; }
    public double GuidanceScale { get; set; }
    public int? Seed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Fps { get; set; }
} 
