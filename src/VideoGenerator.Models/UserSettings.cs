using System.Text.Json.Serialization;

namespace VideoGenerator.Models;

public class UserSettings
{
    // Model Configuration
    public string ModelPath { get; set; } = string.Empty;
    public bool IsModelLoaded { get; set; } = false;
    
    // Generation Parameters
    public string LastPrompt { get; set; } = string.Empty;
    public List<string> RecentPrompts { get; set; } = [];
    public int DurationSeconds { get; set; } = 5;
    public int Steps { get; set; } = 50;
    public double GuidanceScale { get; set; } = 7.5;
    public string Seed { get; set; } = string.Empty;
    public int Width { get; set; } = 576;
    public int Height { get; set; } = 1024;
    public int Fps { get; set; } = 24;
    public string SelectedResolutionName { get; set; } = "Portrait (9:16) - TikTok/Instagram";
    public string LastInputImagePath { get; set; } = string.Empty;
    
    // Application Settings
    public string OutputDirectory { get; set; } = string.Empty;
    
    // UI State (optional)
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    
    // Metadata
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
    
    public UserSettings()
    {
        RecentPrompts = [];
    }
    
    public void AddRecentPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return;
        
        // Remove if already exists
        RecentPrompts.RemoveAll(p => p.Equals(prompt, StringComparison.OrdinalIgnoreCase));
        
        // Add to beginning
        RecentPrompts.Insert(0, prompt);
        
        // Keep only last 10 prompts
        if (RecentPrompts.Count > 10)
        {
            RecentPrompts = RecentPrompts.Take(10).ToList();
        }
        
        LastPrompt = prompt;
    }
} 