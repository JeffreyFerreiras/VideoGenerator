using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using VideoGenerator.Models;
using VideoGenerator.Services;

namespace VideoGenerator.Services.Tests;

[TestFixture]
public class PythonExecutorTests
{
    private PythonExecutor _pythonExecutor;
    private ILogger<PythonExecutor> _logger;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<PythonExecutor>>();
        _pythonExecutor = new PythonExecutor(_logger);
    }

    [TearDown]
    public void TearDown()
    {
        _pythonExecutor?.Dispose();
    }

    [Test]
    public void ExecuteAsync_ThrowsFileNotFoundException_WhenPythonScriptDoesNotExist()
    {
        var request = new PythonGenerationRequest
        {
            ModelPath = "test-model",
            Prompt = "test prompt",
            OutputPath = "test-output.mp4",
            Steps = 10
        };

        Assert.ThrowsAsync<FileNotFoundException>(() => 
            _pythonExecutor.ExecuteAsync(request, CancellationToken.None));
    }

    [Test]
    public void ProgressChanged_EventIsRaised_WhenReportProgressIsCalled()
    {
        VideoGenerationProgressEventArgs? eventArgs = null;
        _pythonExecutor.ProgressChanged += (sender, args) => eventArgs = args;

        var progressProperty = typeof(PythonExecutor).GetMethod("ReportProgress", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        progressProperty?.Invoke(_pythonExecutor, new object[] { 5, 10, "Test message" });

        Assert.That(eventArgs, Is.Not.Null);
        Assert.That(eventArgs.CurrentStep, Is.EqualTo(5));
        Assert.That(eventArgs.TotalSteps, Is.EqualTo(10));
        Assert.That(eventArgs.StatusMessage, Is.EqualTo("Test message"));
    }

    [Test]
    public void TryParseStepProgress_ReturnsTrue_WithValidStepFormat()
    {
        var method = typeof(PythonExecutor).GetMethod("TryParseStepProgress",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var parameters = new object[] { "Step 5 of 10 completed", 0, 0 };
        var result = (bool)method!.Invoke(_pythonExecutor, parameters);
        
        Assert.That(result, Is.True);
        Assert.That(parameters[1], Is.EqualTo(5));  
        Assert.That(parameters[2], Is.EqualTo(10)); 
    }

    [Test]
    public void TryParseStepProgress_ReturnsFalse_WithInvalidFormat()
    {
        var method = typeof(PythonExecutor).GetMethod("TryParseStepProgress",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var parameters = new object[] { "Invalid progress message", 0, 0 };
        var result = (bool)method!.Invoke(_pythonExecutor, parameters);
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsStatusUpdate_ReturnsTrue_WithValidStatusMessages()
    {
        var method = typeof(PythonExecutor).GetMethod("IsStatusUpdate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.That((bool)method!.Invoke(_pythonExecutor, new object[] { "Generating video..." }), Is.True);
        Assert.That((bool)method.Invoke(_pythonExecutor, new object[] { "Loading model..." }), Is.True);
        Assert.That((bool)method.Invoke(_pythonExecutor, new object[] { "Saving video..." }), Is.True);
    }

    [Test]
    public void IsStatusUpdate_ReturnsFalse_WithInvalidStatusMessage()
    {
        var method = typeof(PythonExecutor).GetMethod("IsStatusUpdate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (bool)method!.Invoke(_pythonExecutor, new object[] { "Random message" });
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetPythonScriptPath_ReturnsCorrectPath()
    {
        var method = typeof(PythonExecutor).GetMethod("GetPythonScriptPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (string)method!.Invoke(_pythonExecutor, null);
        
        Assert.That(result, Does.EndWith("ltx_video_generator.py"));
        Assert.That(result, Does.Contain("python"));
    }

    [Test]
    public async Task CreateRequestFileAsync_CreatesValidJsonFile()
    {
        var request = new PythonGenerationRequest
        {
            ModelPath = "test-model",
            Prompt = "test prompt",
            OutputPath = "test-output.mp4",
            Steps = 10,
            GuidanceScale = 7.5,
            Seed = 42
        };

        var method = typeof(PythonExecutor).GetMethod("CreateRequestFileAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var task = (Task<string>)method!.Invoke(_pythonExecutor, new object[] { request, CancellationToken.None });
        var filePath = await task;

        try
        {
            Assert.That(File.Exists(filePath), Is.True);
            var json = await File.ReadAllTextAsync(filePath);
            Assert.That(json, Does.Contain("test-model"));
            Assert.That(json, Does.Contain("test prompt"));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}