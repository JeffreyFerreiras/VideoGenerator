using System.Windows;

namespace VideoGenerator.UI.Services
{
    /// <inheritdoc />
    public class DialogService : IDialogService
    {
        /// <inheritdoc />
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }
    }
}