using Microsoft.Win32;

namespace VideoGenerator.UI.Services
{
    /// <inheritdoc />
    public class FileDialogService : IFileDialogService
    {
        /// <inheritdoc />
        public string? PickFolder(string title, string initialDirectory)
        {
            var dlg = new OpenFolderDialog
            {
                Title = title,
                InitialDirectory = initialDirectory
            };
            return dlg.ShowDialog() == true ? dlg.FolderName : null;
        }

        /// <inheritdoc />
        public string? PickFile(string title, string filter, string initialDirectory)
        {
            var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                InitialDirectory = initialDirectory
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}