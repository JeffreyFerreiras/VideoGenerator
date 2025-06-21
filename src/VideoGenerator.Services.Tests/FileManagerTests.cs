using NUnit.Framework;
using VideoGenerator.Services;

namespace VideoGenerator.Services.Tests;

[TestFixture]
public class FileManagerTests
{
    private FileManager _fileManager;

    [SetUp]
    public void SetUp()
    {
        _fileManager = new FileManager();
    }

    [Test]
    public void GenerateOutputPath_WithNullOutputDirectory_UsesCurrentDirectory()
    {
        var result = _fileManager.GenerateOutputPath(null);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.StartWith(Environment.CurrentDirectory));
        Assert.That(result, Does.EndWith(".mp4"));
        Assert.That(result, Does.Contain("generated_video_"));
    }

    [Test]
    public void GenerateOutputPath_WithEmptyOutputDirectory_UsesCurrentDirectory()
    {
        var result = _fileManager.GenerateOutputPath("");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.StartWith(Environment.CurrentDirectory));
        Assert.That(result, Does.EndWith(".mp4"));
        Assert.That(result, Does.Contain("generated_video_"));
    }

    [Test]
    public void GenerateOutputPath_WithValidOutputDirectory_UsesProvidedDirectory()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "test_output");
        
        var result = _fileManager.GenerateOutputPath(outputDir);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.StartWith(outputDir));
        Assert.That(result, Does.EndWith(".mp4"));
        Assert.That(result, Does.Contain("generated_video_"));
        Assert.That(Directory.Exists(outputDir), Is.True);
        
        Directory.Delete(outputDir, true);
    }

    [Test]
    public void GenerateOutputPath_CreatesUniqueFilenames()
    {
        var result1 = _fileManager.GenerateOutputPath(null);
        Thread.Sleep(1000); // Ensure timestamp difference
        var result2 = _fileManager.GenerateOutputPath(null);
        
        Assert.That(result1, Is.Not.EqualTo(result2));
    }

    [Test]
    public void GenerateOutputPath_ContainsTimestamp()
    {
        var result = _fileManager.GenerateOutputPath(null);
        var fileName = Path.GetFileNameWithoutExtension(result);
        
        Assert.That(fileName, Does.Match(@"generated_video_\d{8}_\d{6}"));
    }
}