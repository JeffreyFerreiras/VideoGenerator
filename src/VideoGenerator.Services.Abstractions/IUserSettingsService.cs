using VideoGenerator.Models;

namespace VideoGenerator.Services.Abstractions;

public interface IUserSettingsService
{
    /// <summary>
    /// Loads user settings from persistent storage
    /// </summary>
    /// <returns>User settings or default settings if load fails</returns>
    Task<UserSettings> LoadSettingsAsync();
    
    /// <summary>
    /// Saves user settings to persistent storage
    /// </summary>
    /// <param name="settings">Settings to save</param>
    Task SaveSettingsAsync(UserSettings settings);
    
    /// <summary>
    /// Gets the current settings (cached in memory)
    /// </summary>
    UserSettings CurrentSettings { get; }
    
    /// <summary>
    /// Updates a specific setting and saves to storage
    /// </summary>
    /// <param name="updateAction">Action to update the settings</param>
    Task UpdateSettingsAsync(Action<UserSettings> updateAction);
    
    /// <summary>
    /// Event raised when settings are changed
    /// </summary>
    event EventHandler<UserSettings>? SettingsChanged;
} 