# VideoGenerator Tests

This directory contains comprehensive tests for the VideoGenerator application, covering both C# and Python components.

## Structure

### C# Tests
- **VideoGenerator.Services.Tests** - Unit tests for the service layer
- **VideoGenerator.UI.Tests** - Unit tests for the UI layer and view models

### Python Tests
- **python/test_ltx_video_generator_practical.py** - Practical tests for the Python LTX video generator

## Running Tests

### C# Tests
Run all C# tests from the project root:
```bash
dotnet test
```

### Python Tests
Run Python tests from the python tests directory:
```bash
cd tests/python
python test_ltx_video_generator_practical.py
```

### Running All Tests
From the project root:
```bash
# Run C# tests
dotnet test

# Run Python tests
cd tests/python && python test_ltx_video_generator_practical.py && cd ../..
```

## Test Coverage

### Python Tests Cover:
- ✅ JSON request loading and validation
- ✅ Argument parsing logic
- ✅ File path validation (existence, file vs directory)
- ✅ Model validation (model_index.json checking)
- ✅ Request parameter validation
- ✅ Error handling scenarios
- ✅ Pipeline selection logic (text-to-video vs image-to-video)
- ✅ File operations and calculations

### C# Tests Cover:
- ✅ Service layer functionality
- ✅ Model management
- ✅ Python executor integration
- ✅ File management operations
- ✅ UI view models and converters
- ✅ Dependency injection setup

## Test Principles Applied

The tests follow SOLID principles and best practices:

- **Single Responsibility**: Each test class focuses on one component
- **Dependency Injection**: Proper mocking of dependencies
- **Interface Segregation**: Tests target specific interfaces
- **Open/Closed**: Tests are extensible for new scenarios
- **Pure Functions**: Most tests verify predictable input/output behavior

## Notes

The Python tests are designed to be practical and avoid complex dependency issues with PyTorch and Diffusers packages. They focus on testing the core logic that can be reliably verified without requiring heavy ML dependencies. 