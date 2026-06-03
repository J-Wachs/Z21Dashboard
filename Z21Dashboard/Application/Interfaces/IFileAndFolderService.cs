namespace Z21Dashboard.Application.Interfaces;

/// <summary>
/// Defines methods for displaying file and folder pickers to the user and retrieving the selected path.
/// </summary>
/// <remarks>Implementations of this interface typically present a UI dialog to the user for selecting a folder or
/// specifying a file path. Methods return null if the user cancels the operation.</remarks>
internal interface IFileAndFolderService
{
    /// <summary>
    /// Displays a dialog that allows the user to select a folder.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the path of the selected folder, or
    /// null if the user cancels the operation.</returns>
    Task<string?> PickFolderAsync();

    /// <summary>
    /// Displays a file save dialog that allows the user to specify a file name and location, and saves the file.
    /// </summary>
    /// <param name="initialFolder">The initial folder path that the dialog will display when opened. If null or empty, the dialog uses the default
    /// folder.</param>
    /// <param name="fileName">The suggested file name to display in the dialog. Can be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the full path of the saved file if
    /// the user completes the dialog; otherwise, null.</returns>
    Task<string?> FileSaveAsAsync(string initialFolder, string fileName);
}
