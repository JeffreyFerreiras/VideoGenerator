namespace VideoGenerator.Services;

// Interfaces are now defined in VideoGenerator.Services.Abstractions

// Simple, focused implementations
public class FileManager : IFileManager
{
    public string GenerateOutputPath(string? outputDirectory)
    {
        var outputDir = string.IsNullOrEmpty(outputDirectory) 
            ? Environment.CurrentDirectory
            : outputDirectory;
            
        Directory.CreateDirectory(outputDir);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = $"generated_video_{timestamp}.mp4";
        
        return Path.Combine(outputDir, fileName);
    }
}
