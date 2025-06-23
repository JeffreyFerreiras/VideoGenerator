using System.Text.Json.Serialization;

namespace VideoGenerator.Models;

public class PythonGenerationRequest
{
    [JsonPropertyName("model_path")]
    public string ModelPath { get; set; } = string.Empty;
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
    
    [JsonPropertyName("output_path")]
    public string OutputPath { get; set; } = string.Empty;
    
    [JsonPropertyName("duration_seconds")]
    public int DurationSeconds { get; set; }
    
    [JsonPropertyName("steps")]
    public int Steps { get; set; }
    
    [JsonPropertyName("guidance_scale")]
    public double GuidanceScale { get; set; }
    
    [JsonPropertyName("seed")]
    public int? Seed { get; set; }
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("fps")]
    public int Fps { get; set; }
    
    [JsonPropertyName("input_image")]
    public string? InputImage { get; set; }
} 
