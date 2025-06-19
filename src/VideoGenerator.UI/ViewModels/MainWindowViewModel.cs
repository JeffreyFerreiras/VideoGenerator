using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using VideoGenerator.Core.Interfaces;
using VideoGenerator.Core.Models;

namespace VideoGenerator.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IVideoGenerationService _videoGenerationService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _modelPath = @"D:\ai-models\Video\Lightbricks-LTX-Video\ltxv-13b-0.9.7-distilled.safetensors";

    [ObservableProperty]
    private bool _isModelLoaded = false;

    [ObservableProperty]
    private string _prompt = "A cute fluffy orange kitten playing with a colorful ball of yarn in a cozy living room, soft natural lighting, adorable and playful movement";

    [ObservableProperty]
    private int _durationSeconds = 5;

    [ObservableProperty]
    private int _steps = 50;

    [ObservableProperty]
    private double _guidanceScale = 7.5;

    [ObservableProperty]
    private string _seed = string.Empty;

    [ObservableProperty]
    private int _width = 576;  // 9:16 aspect ratio for TikTok (576x1024)

    [ObservableProperty]
    private int _height = 1024; // Vertical format for TikTok

    [ObservableProperty]
    private int _fps = 30;  // Higher FPS for smooth TikTok content

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _lastGeneratedVideoPath;

    public ObservableCollection<VideoGenerationResult> GenerationHistory { get; } = new();

    public MainWindowViewModel(IVideoGenerationService videoGenerationService, ILogger<MainWindowViewModel> logger)
    {
        _videoGenerationService = videoGenerationService ?? throw new ArgumentNullException(nameof(videoGenerationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to progress events
        _videoGenerationService.ProgressChanged += OnVideoGenerationProgressChanged;

        // Set output directory to videos folder in build directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        OutputDirectory = Path.Combine(appDir, "videos");
        
        // Ensure the videos directory exists
        Directory.CreateDirectory(OutputDirectory);
    }

    [RelayCommand]
    private async Task LoadModelAsync()
    {
        try
        {
            StatusMessage = "Loading model...";
            _logger.LogInformation("Loading model from: {ModelPath}", ModelPath);

            var success = await _videoGenerationService.LoadModelAsync(ModelPath);
            IsModelLoaded = success;

            if (success)
            {
                StatusMessage = "Model loaded successfully";
                _logger.LogInformation("Model loaded successfully");
            }
            else
            {
                StatusMessage = "Failed to load model";
                _logger.LogError("Failed to load model");
                MessageBox.Show("Failed to load the model. Please check the model path and try again.", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model");
            StatusMessage = "Error loading model";
            MessageBox.Show($"Error loading model: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void BrowseModelPath()
    {
        // Ask user whether they want to select a directory or file
        var result = MessageBox.Show(
            "Do you want to select a model DIRECTORY (recommended) or a single file?\n\n" +
            "• YES = Select Directory (complete model with all components)\n" +
            "• NO = Select File (may be missing components)\n" +
            "• CANCEL = Cancel",
            "Model Selection Type", 
            MessageBoxButton.YesNoCancel, 
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
            return;

        if (result == MessageBoxResult.Yes)
        {
            // Directory selection (recommended)
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select LTX Video Model Directory",
                InitialDirectory = Path.GetDirectoryName(ModelPath)
            };

            if (folderDialog.ShowDialog() == true)
            {
                ModelPath = folderDialog.FolderName;
            }
        }
        else
        {
            // File selection (fallback)
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select LTX Video Model File",
                Filter = "SafeTensors Files (*.safetensors)|*.safetensors|All Files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(ModelPath)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ModelPath = openFileDialog.FileName;
            }
        }
    }

    [RelayCommand]
    private void ShowModelDownloadHelp()
    {
        var helpMessage = @"To use LTX Video, you need to download a COMPLETE model directory (not just single files):

🎯 SOLUTION: Download Complete Model Directory

1. RECOMMENDED: Use Git to clone the full repository
   • Install Git: https://git-scm.com/download/win
   • Open Command Prompt and run:
     git clone https://huggingface.co/Lightricks/LTX-Video
   • This downloads ALL required files (~6-30GB depending on variant)

2. Alternative: Manual download from Hugging Face
   • Go to: https://huggingface.co/Lightricks/LTX-Video
   • Click 'Files and versions' tab
   • Download ALL files in the repository (not just .safetensors)
   • Maintain the exact folder structure

3. Set model path to the DIRECTORY (not a single file)
   • Example: D:\ai-models\LTX-Video\ (directory)
   • NOT: D:\ai-models\model.safetensors (single file)

⚠️ Single .safetensors files are missing required components like T5EncoderModel!";

        MessageBox.Show(helpMessage, "Model Download Guide", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    [RelayCommand(CanExecute = nameof(CanGenerateVideo))]
    private async Task GenerateVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            MessageBox.Show("Please enter a prompt for video generation.", "Validation Error", 
                           MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsGenerating = true;
            ProgressValue = 0;
            StatusMessage = "Preparing video generation...";
            _cancellationTokenSource = new CancellationTokenSource();

            var request = new VideoGenerationRequest
            {
                Prompt = Prompt,
                DurationSeconds = DurationSeconds,
                Steps = Steps,
                GuidanceScale = GuidanceScale,
                Seed = string.IsNullOrWhiteSpace(Seed) ? null : int.Parse(Seed),
                Width = Width,
                Height = Height,
                Fps = Fps,
                OutputDirectory = OutputDirectory
            };

            _logger.LogInformation("Starting video generation with prompt: {Prompt}", Prompt);

            var result = await _videoGenerationService.GenerateVideoAsync(request, _cancellationTokenSource.Token);

            if (result.IsSuccess)
            {
                LastGeneratedVideoPath = result.VideoFilePath;
                StatusMessage = $"Video generated successfully! ({result.ProcessingTime:mm\\:ss})";
                _logger.LogInformation("Video generation completed: {VideoPath}", result.VideoFilePath);

                GenerationHistory.Insert(0, result);

                MessageBox.Show($"Video generated successfully!\nSaved to: {result.VideoFilePath}", 
                               "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "Video generation failed";
                _logger.LogError("Video generation failed: {Error}", result.ErrorMessage);
                MessageBox.Show($"Video generation failed: {result.ErrorMessage}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Video generation cancelled";
            _logger.LogInformation("Video generation was cancelled");
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during video generation";
            _logger.LogError(ex, "Error during video generation");
            MessageBox.Show($"Error during video generation: {ex.Message}", 
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsGenerating = false;
            ProgressValue = 0;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _cancellationTokenSource?.Cancel();
        StatusMessage = "Cancelling generation...";
    }

    [RelayCommand]
    private void OpenGeneratedVideo()
    {
        if (!string.IsNullOrEmpty(LastGeneratedVideoPath) && File.Exists(LastGeneratedVideoPath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LastGeneratedVideoPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open video file");
                MessageBox.Show($"Failed to open video file: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void OpenOutputDirectory()
    {
        if (Directory.Exists(OutputDirectory))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = OutputDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open output directory");
                MessageBox.Show($"Failed to open output directory: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CanGenerateVideo()
    {
        return IsModelLoaded && !IsGenerating && !string.IsNullOrWhiteSpace(Prompt);
    }

    private void OnVideoGenerationProgressChanged(object? sender, VideoGenerationProgressEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ProgressValue = e.ProgressPercentage;
            StatusMessage = e.StatusMessage ?? $"Step {e.CurrentStep} of {e.TotalSteps}";
        });
    }

    partial void OnPromptChanged(string value)
    {
        GenerateVideoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsModelLoadedChanged(bool value)
    {
        GenerateVideoCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsGeneratingChanged(bool value)
    {
        GenerateVideoCommand.NotifyCanExecuteChanged();
    }
} 