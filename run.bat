@echo off
echo === LTX Video Generator Setup ===

REM Check if Python is installed
python --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.9 or later from https://python.org
    echo Make sure to check "Add Python to PATH" during installation
    pause
    exit /b 1
)

echo Python found, checking environment...

REM Setup Python environment if needed
if exist src\python\setup_python.py (
    echo Checking Python dependencies...
    python src\python\setup_python.py --check-deps
    if %ERRORLEVEL% neq 0 (
        echo Python dependencies not installed or incomplete.
        echo.
        choice /C YN /M "Would you like to install Python dependencies now? (This may take several minutes)"
        if errorlevel 2 goto :skip_python_setup
        if errorlevel 1 goto :install_python_deps
        
        :install_python_deps
        echo Installing Python dependencies... (This may take several minutes)
        echo NOTE: You may see permission warnings - these are usually safe to ignore
        python src\python\setup_python.py
        if %ERRORLEVEL% neq 0 (
            echo.
            echo WARNING: Python environment setup encountered some issues.
            echo The application may still work if most dependencies are installed.
            echo.
            choice /C YN /M "Continue anyway?"
            if errorlevel 2 goto :end
        )
        
        :skip_python_setup
        echo.
    ) else (
        echo Python dependencies are ready!
    )
) else (
    echo Python setup script not found, skipping environment setup...
)

echo.
echo Starting LTX Video Generator...
dotnet run --project src/VideoGenerator.UI
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Failed to start the application
    echo Make sure .NET 8.0 SDK is installed
    pause
    goto :end
)

:end 