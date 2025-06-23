using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoGenerator.Models;
using VideoGenerator.Services.Abstractions;

namespace VideoGenerator.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly ILogger<UserSettingsService> _logger;
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private UserSettings _currentSettings;
    private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

    public event EventHandler<UserSettings>? SettingsChanged;

    public UserSettings CurrentSettings => _currentSettings;

    public UserSettingsService(ILogger<UserSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Setup paths
        _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoGenerator");
        _settingsFilePath = Path.Combine(_settingsDirectory, "user-settings.json");
        
        // JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        // Initialize with default settings
        _currentSettings = new UserSettings();
        
        _logger.LogInformation("UserSettingsService initialized. Settings path: {SettingsPath}", _settingsFilePath);
    }

    public async Task<UserSettings> LoadSettingsAsync()
    {
        try
        {
            _logger.LogInformation("Loading user settings from: {SettingsPath}", _settingsFilePath);
            
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("Settings file not found. Using default settings.");
                _currentSettings = CreateDefaultSettings();
                return _currentSettings;
            }

            var jsonContent = await File.ReadAllTextAsync(_settingsFilePath);
            
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Settings file is empty. Using default settings.");
                _currentSettings = CreateDefaultSettings();
                return _currentSettings;
            }

            var loadedSettings = JsonSerializer.Deserialize<UserSettings>(jsonContent, _jsonOptions);
            
            if (loadedSettings == null)
            {
                _logger.LogWarning("Failed to deserialize settings. Using default settings.");
                _currentSettings = CreateDefaultSettings();
                return _currentSettings;
            }

            _currentSettings = loadedSettings;
            _logger.LogInformation("User settings loaded successfully. Last saved: {LastSaved}", _currentSettings.LastSaved);
            
            return _currentSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user settings. Using default settings.");
            _currentSettings = CreateDefaultSettings();
            return _currentSettings;
        }
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        
        await _saveSemaphore.WaitAsync();
        
        try
        {
            _logger.LogDebug("Saving user settings to: {SettingsPath}", _settingsFilePath);
            
            // Ensure directory exists
            Directory.CreateDirectory(_settingsDirectory);
            
            // Update metadata
            settings.LastSaved = DateTime.UtcNow;
            
            // Serialize to JSON
            var jsonContent = JsonSerializer.Serialize(settings, _jsonOptions);
            
            // Write to file
            await File.WriteAllTextAsync(_settingsFilePath, jsonContent);
            
            // Update current settings
            _currentSettings = settings;
            
            _logger.LogInformation("User settings saved successfully");
            
            // Raise event
            SettingsChanged?.Invoke(this, _currentSettings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user settings to: {SettingsPath}", _settingsFilePath);
            throw;
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    public async Task UpdateSettingsAsync(Action<UserSettings> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);
        
        try
        {
            // Create a copy to modify
            var settingsCopy = JsonSerializer.Deserialize<UserSettings>(
                JsonSerializer.Serialize(_currentSettings, _jsonOptions), 
                _jsonOptions) ?? new UserSettings();
            
            // Apply updates
            updateAction(settingsCopy);
            
            // Save the updated settings
            await SaveSettingsAsync(settingsCopy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user settings");
            throw;
        }
    }

    private UserSettings CreateDefaultSettings()
    {
        var settings = new UserSettings();
        
        // Set default output directory to videos folder in app directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        settings.OutputDirectory = Path.Combine(appDir, "videos");
        
        _logger.LogInformation("Created default user settings");
        return settings;
    }

    public void Dispose()
    {
        _saveSemaphore?.Dispose();
    }
} 