# LTX Video Generator

A modern C# WPF application for generating videos using the LTX Video model. This application provides an intuitive interface to interact with the Lightricks LTX-Video model for creating videos from text prompts.

## Features

- **Modern WPF Interface**: Clean, professional UI with modern styling
- **LTX Video Model Integration**: Uses Python interop to run the LTX Video model
- **Real-time Progress Tracking**: Monitor video generation progress
- **Generation History**: Keep track of previously generated videos
- **Flexible Parameters**: Control video duration, quality, dimensions, and more
- **File Management**: Easy browsing for model files and output directories

## Prerequisites

### .NET Requirements
- .NET 8.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (recommended)

### Python Requirements
- Python 3.9 or later
- **CUDA-capable GPU (REQUIRED for reasonable performance)**
  - NVIDIA RTX 20-series or newer recommended
  - At least 8GB VRAM (16GB+ recommended for high quality)
  - CUDA 11.8 or 12.1 compatible drivers

## Setup Instructions

### 1. Install Python (Required)

Download and install Python 3.9 or later from [python.org](https://python.org). 
**Important**: Make sure to check "Add Python to PATH" during installation.

### 2. Download the LTX Video Model

Download the LTX Video model from Hugging Face or the official Lightricks repository. The application expects:
- Model file: `ltxv-13b-0.9.7-distilled.safetensors`
- Default location: `D:\ai-models\Video\Lightbricks-LTX-Video\`

### 3. Build the Application

```bash
# Clone the repository
git clone <repository-url>
cd video_generator

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build --configuration Release
```

### 4. Run the Application

**Easy Way (Recommended):**
```bash
# Double-click run.bat or execute:
run.bat
```

**Manual Way:**
```bash
# Setup Python environment (first time only)
python src/python/setup_python.py

# Run the application
dotnet run --project src/VideoGenerator.UI
```

**GPU Setup (CRITICAL for Performance):**
```bash
# Install CUDA-enabled PyTorch (REQUIRED for fast generation)
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121

# Install other dependencies
pip install -r python_requirements.txt

# Test GPU is working
python -c "import torch; print('CUDA available:', torch.cuda.is_available()); print('GPU:', torch.cuda.get_device_name(0) if torch.cuda.is_available() else 'None')"
```

⚠️ **IMPORTANT**: Without GPU acceleration, video generation will take 30-60+ minutes instead of 2-5 minutes!

The application will automatically:
- Check if Python is installed
- Verify Python dependencies
- Set up the environment if needed
- Guide you through any missing requirements

## Usage

1. **Load Model**: 
   - Click "Browse..." to select your LTX Video model file
   - Click "Load Model" to initialize the model

2. **Configure Generation Parameters**:
   - **Prompt**: Enter your text description for the video
   - **Duration**: Set video length in seconds (1-30)
   - **Steps**: Number of inference steps (higher = better quality, slower)
   - **Guidance Scale**: Controls adherence to prompt (7.5 recommended)
   - **Dimensions**: Set width and height (512x512 recommended)
   - **FPS**: Frames per second for output video
   - **Seed**: Optional seed for reproducible results

3. **Generate Video**:
   - Click "Generate Video" to start the process
   - Monitor progress in the status panel
   - Cancel generation if needed

4. **View Results**:
   - Click "Open Last Video" to view the generated video
   - Click "Open Output Folder" to browse all generated videos
   - Check generation history in the right panel

## Architecture

The application follows clean architecture principles with dependency injection:

```
├── VideoGenerator.Models/        # Domain models and interfaces
├── VideoGenerator.Services/      # Python interop and business logic
├── VideoGenerator.UI/           # WPF presentation layer
│   ├── ViewModels/             # MVVM view models
│   ├── Styles/                 # UI styling and themes
│   └── Converters/             # Data binding converters
└── python/                      # Python scripts and environment
    ├── ltx_video_generator.py  # Main video generation script
    └── setup_python.py        # Environment setup script
```

### Key Components

- **IVideoGenerationService**: Interface for video generation operations
- **PythonVideoGenerationService**: Implements video generation via Python subprocess
- **IPythonEnvironmentService**: Manages Python environment setup and validation
- **MainWindowViewModel**: MVVM view model with reactive properties
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Modular Python Scripts**: Separate Python files for video generation and setup

## Configuration

### Model Path
The application defaults to:
```
D:\ai-models\Video\Lightbricks-LTX-Video\ltxv-13b-0.9.7-distilled.safetensors
```

You can change this path in the UI or modify the default in `MainWindowViewModel.cs`.

### Output Directory
Videos are saved to the same directory as the model by default. You can specify a different output directory in the UI.

## Performance Tips

1. **🚀 GPU Acceleration (CRITICAL)**: 
   - **MUST HAVE**: CUDA-enabled PyTorch installation
   - **CPU Generation**: 30-60+ minutes per video (unusably slow)
   - **GPU Generation**: 2-5 minutes per video (acceptable)
   - Verify with: `python -c "import torch; print(torch.cuda.is_available())"`

2. **VRAM Usage**: 
   - Monitor GPU memory usage; reduce resolution if needed
   - 8GB VRAM: 512x512 or 720p
   - 16GB+ VRAM: 1080p or higher resolutions

3. **Generation Settings**:
   - **Steps**: 20-30 for preview, 50+ for final quality
   - **Resolution**: Start with 576x1024 (TikTok) for best speed/quality balance
   - **Duration**: 3-5 seconds recommended

4. **Storage**: Ensure sufficient disk space for generated videos

## Troubleshooting

### Common Issues

**Model Loading Fails**:
- Verify the model file path is correct
- Ensure the model file is not corrupted
- Check Python dependencies are installed

**Generation Fails**:
- Verify Python is in your system PATH
- Check CUDA installation if using GPU
- Monitor system resources (RAM, VRAM, disk space)

**Python Process Errors**:
- Install Python dependencies: `pip install -r python_requirements.txt`
- Verify Python version compatibility (3.9+)
- Check firewall/antivirus settings

### Logs
The application logs to the console. Check the output window in your IDE or run from command line to see detailed logs.

## Development

### Adding New Features
1. Follow SOLID principles
2. Use dependency injection for services
3. Implement proper error handling
4. Add unit tests for business logic
5. Follow MVVM pattern for UI components

### Testing
Run tests with:
```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Follow the coding standards
4. Add tests for new functionality
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Lightricks for the LTX Video model
- Microsoft for .NET and WPF
- Hugging Face for the Diffusers library
- The open-source community for various dependencies 