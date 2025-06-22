using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using VideoGenerator.Services;
using FluentAssertions;

namespace VideoGenerator.Services.Tests;

[TestFixture]
public class ModelManagerTests
{
    private ModelManager _modelManager;
    private ILogger<ModelManager> _logger;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<ModelManager>>();
        _modelManager = new ModelManager(_logger);
    }

    [Test]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ModelManager(null!));
    }

    [Test]
    public async Task IsLoadedAsync_ReturnsFalse_WhenModelNotLoaded()
    {
        var result = await _modelManager.IsLoadedAsync();
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void LoadAsync_ThrowsArgumentException_WhenModelPathIsNull()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _modelManager.LoadAsync(null!));
    }

    [Test]
    public void LoadAsync_ThrowsArgumentException_WhenModelPathIsEmpty()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _modelManager.LoadAsync(""));
    }

    [Test]
    public void LoadAsync_ThrowsArgumentException_WhenModelPathIsWhitespace()
    {
        Assert.ThrowsAsync<ArgumentException>(() => _modelManager.LoadAsync("   "));
    }

    [Test]
    public async Task LoadAsync_ReturnsFalse_WhenModelPathDoesNotExist()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var result = await _modelManager.LoadAsync(nonExistentPath);
        
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task LoadAsync_ReturnsTrue_WhenValidFilePathExists()
    {
        var tempFile = Path.GetTempFileName();
        
        try
        {
            var result = await _modelManager.LoadAsync(tempFile);
            
            Assert.That(result, Is.True);
            Assert.That(await _modelManager.IsLoadedAsync(), Is.True);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task LoadAsync_ReturnsTrue_WhenValidDirectoryPathExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var result = await _modelManager.LoadAsync(tempDir);
            
            Assert.That(result, Is.True);
            Assert.That(await _modelManager.IsLoadedAsync(), Is.True);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task GetModelPath_ThrowsInvalidOperationException_WhenModelNotLoaded()
    {
        Assert.Throws<InvalidOperationException>(() => _modelManager.GetModelPath());
    }

    [Test]
    public async Task GetModelPath_ReturnsCorrectPath_WhenModelIsLoaded()
    {
        var tempFile = Path.GetTempFileName();
        
        try
        {
            await _modelManager.LoadAsync(tempFile);
            
            var result = _modelManager.GetModelPath();
            
            Assert.That(result, Is.EqualTo(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task LoadAsync_CallsLoggerWhenModelLoadedSuccessfully()
    {
        var tempFile = Path.GetTempFileName();
        
        try
        {
            var result = await _modelManager.LoadAsync(tempFile);
            
            Assert.That(result, Is.True);
            _logger.ReceivedCalls().Count().Should().BeGreaterThan(0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task LoadAsync_CallsLoggerWhenModelPathDoesNotExist()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var result = await _modelManager.LoadAsync(nonExistentPath);
        
        Assert.That(result, Is.False);
        _logger.ReceivedCalls().Count().Should().BeGreaterThan(0);
    }

    [Test]
    public async Task LoadAsync_CallsLoggerWhenDirectoryMissingModelIndex()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var result = await _modelManager.LoadAsync(tempDir);
            
            Assert.That(result, Is.True);
            _logger.ReceivedCalls().Count().Should().BeGreaterThan(0);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}