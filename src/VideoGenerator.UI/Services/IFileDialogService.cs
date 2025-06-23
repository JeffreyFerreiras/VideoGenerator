namespace VideoGenerator.UI.Services
{
    /// <summary>
    /// Abstraction for file/folder selection dialogs to allow for testability.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Shows a folder picker and returns the selected folder path, or null if cancelled.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="initialDirectory">Initial directory path.</param>
        /// <returns>Selected folder path or null.</returns>
        string? PickFolder(string title, string initialDirectory);

        /// <summary>
        /// Shows an open-file dialog and returns the selected file path, or null if cancelled.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="filter">File filter string.</param>
        /// <param name="initialDirectory">Initial directory path.</param>
        /// <returns>Selected file path or null.</returns>
        string? PickFile(string title, string filter, string initialDirectory);
    }
}