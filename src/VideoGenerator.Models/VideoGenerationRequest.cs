using System.ComponentModel.DataAnnotations;

namespace VideoGenerator.Models;
public class VideoGenerationRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Prompt { get; set; } = string.Empty;

    [Range(1, 30)]
    public int DurationSeconds { get; set; } = 5;

    [Range(1, 100)]
    public int Steps { get; set; } = 50;

    [Range(0.1, 2.0)]
    public double GuidanceScale { get; set; } = 7.5;

    public int? Seed { get; set; }

    [Range(256, 1536)]  // Extended range to support TikTok dimensions
    public int Width { get; set; } = 576;  // 9:16 aspect ratio for TikTok

    [Range(256, 1536)]  // Extended range to support TikTok dimensions
    public int Height { get; set; } = 1024; // Vertical format for TikTok

    [Range(1, 60)]
    public int Fps { get; set; } = 30;  // Higher FPS for smooth TikTok content

    public string OutputDirectory { get; set; } = string.Empty;
    
    public string? InputImagePath { get; set; }
} 