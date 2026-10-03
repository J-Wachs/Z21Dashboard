using Windows.Storage.Pickers;
using Windows.Storage;
using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard.Application.Services;

/// <summary>
/// Services to communicate with the pc's file system.
/// </summary>
public class FileAndFolderService : IFileAndFolderService
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync()
    {
#if WINDOWS
        var folderPicker = new FolderPicker();
        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Microsoft.Maui.Controls.Application.Current?.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        StorageFolder? folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
#else
        return null;
#endif
    }

    /// <inheritdoc />
    public async Task<string?> FileSaveAsAsync(string initialFolder, string fileName)
    {
        var fileSavePicker = new FileSavePicker();
        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Microsoft.Maui.Controls.Application.Current?.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);
        fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        fileSavePicker.SuggestedFileName = fileName;
        fileSavePicker.DefaultFileExtension = ".json";
        fileSavePicker.FileTypeChoices.Add("File", [.. new List<string>() { ".json" }]);
        StorageFile? file = await fileSavePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
