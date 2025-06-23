using System.Windows;

namespace VideoGenerator.UI.Services
{
    /// <summary>
    /// Abstraction for message dialogs to allow for testability.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a message box with specified text, caption, buttons, and icon.
        /// </summary>
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}