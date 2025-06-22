using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using VideoGenerator.Models;
using VideoGenerator.Services;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services.Tests;

[TestFixture]
public class PythonVideoGenerationServiceTests
{
    private PythonVideoGenerationService _service;
    private ILogger<PythonVideoGenerationService> _logger;
    private IPythonExecutor _pythonExecutor;
    private IFileManager _fileManager;
    private IModelManager _modelManager;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<PythonVideoGenerationService>>();
        _pythonExecutor = Substitute.For<IPythonExecutor>();
        _fileManager = Substitute.For<IFileManager>();
        _modelManager = Substitute.For<IModelManager>();
        
        _service = new PythonVideoGenerationService(_logger, _pythonExecutor, _fileManager, _modelManager);
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new PythonVideoGenerationService(null!, _pythonExecutor, _fileManager, _modelManager));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenPythonExecutorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new PythonVideoGenerationService(_logger, null!, _fileManager, _modelManager));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenFileManagerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new PythonVideoGenerationService(_logger, _pythonExecutor, null!, _modelManager));
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenModelManagerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new PythonVideoGenerationService(_logger, _pythonExecutor, _fileManager, null!));
    }

    [Test]
    public async Task IsModelLoadedAsync_ReturnsModelManagerResult()
    {
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(true));
        
        var result = await _service.IsModelLoadedAsync();
        
        Assert.That(result, Is.True);
        await _modelManager.Received(1).IsLoadedAsync();
    }

    [Test]
    public async Task LoadModelAsync_ReturnsModelManagerResult()
    {
        var modelPath = "test-model-path";
        _modelManager.LoadAsync(modelPath).Returns(Task.FromResult(true));
        
        var result = await _service.LoadModelAsync(modelPath);
        
        Assert.That(result, Is.True);
        await _modelManager.Received(1).LoadAsync(modelPath);
    }

    [Test]
    public void GenerateVideoAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.GenerateVideoAsync(null!));
    }

    [Test]
    public async Task GenerateVideoAsync_ReturnsFailure_WhenModelNotLoaded()
    {
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(false));
        var request = new VideoGenerationRequest { Prompt = "test" };
        
        var result = await _service.GenerateVideoAsync(request);
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Model is not loaded"));
    }

    [Test]
    public async Task GenerateVideoAsync_ReturnsSuccess_WhenGenerationCompletes()
    {
        var request = new VideoGenerationRequest 
        { 
            Prompt = "test prompt",
            OutputDirectory = "test-output"
        };
        var outputPath = "test-output.mp4";
        
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(true));
        _modelManager.GetModelPath().Returns("model-path");
        _fileManager.GenerateOutputPath(request.OutputDirectory).Returns(outputPath);
        _pythonExecutor.ExecuteAsync(Arg.Any<PythonGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        File.WriteAllText(outputPath, "fake video content");
        
        try
        {
            var result = await _service.GenerateVideoAsync(request);
            
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.VideoFilePath, Is.EqualTo(outputPath));
            Assert.That(result.Prompt, Is.EqualTo(request.Prompt));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Test]
    public async Task GenerateVideoAsync_ReturnsFailure_WhenOutputFileNotFound()
    {
        var request = new VideoGenerationRequest { Prompt = "test" };
        var outputPath = "non-existent-output.mp4";
        
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(true));
        _modelManager.GetModelPath().Returns("model-path");
        _fileManager.GenerateOutputPath(Arg.Any<string>()).Returns(outputPath);
        _pythonExecutor.ExecuteAsync(Arg.Any<PythonGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        var result = await _service.GenerateVideoAsync(request);
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Generated video file not found"));
    }

    [Test]
    public async Task GenerateVideoAsync_ReturnsFailure_WhenCancellationRequested()
    {
        var request = new VideoGenerationRequest { Prompt = "test" };
        var cancellationToken = new CancellationToken(true);
        
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(true));
        _pythonExecutor.ExecuteAsync(Arg.Any<PythonGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cancellationToken));
        
        var result = await _service.GenerateVideoAsync(request, cancellationToken);
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("cancelled"));
    }

    [Test]
    public async Task GenerateVideoAsync_ReturnsFailure_WhenExceptionThrown()
    {
        var request = new VideoGenerationRequest { Prompt = "test" };
        var exception = new InvalidOperationException("Test exception");
        
        _modelManager.IsLoadedAsync().Returns(Task.FromResult(true));
        _pythonExecutor.ExecuteAsync(Arg.Any<PythonGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));
        
        var result = await _service.GenerateVideoAsync(request);
        
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Test exception"));
    }

    [Test]
    public void ProgressChanged_EventIsForwarded_FromPythonExecutor()
    {
        VideoGenerationProgressEventArgs? receivedArgs = null;
        _service.ProgressChanged += (sender, args) => receivedArgs = args;
        
        var progressArgs = new VideoGenerationProgressEventArgs
        {
            CurrentStep = 5,
            TotalSteps = 10,
            StatusMessage = "Test progress"
        };
        
        _pythonExecutor.ProgressChanged += Raise.Event<EventHandler<VideoGenerationProgressEventArgs>>(
            _pythonExecutor, progressArgs);
        
        Assert.That(receivedArgs, Is.Not.Null);
        Assert.That(receivedArgs.CurrentStep, Is.EqualTo(5));
        Assert.That(receivedArgs.TotalSteps, Is.EqualTo(10));
        Assert.That(receivedArgs.StatusMessage, Is.EqualTo("Test progress"));
    }
}