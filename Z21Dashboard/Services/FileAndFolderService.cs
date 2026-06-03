using Windows.Storage.Pickers;
using Windows.Storage;
using Z21Dashboard.Application.Interfaces;

namespace Z21Dashboard.Services;

public class FileAndFolderService : IFileAndFolderService
{
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

    public async Task<string?> FileSaveAsAsync(string initialFolder, string fileName)
    {
        var fileSavePicker = new FileSavePicker();
        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(Microsoft.Maui.Controls.Application.Current?.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window);
        WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);
        fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        fileSavePicker.SuggestedFileName = fileName;
        fileSavePicker.DefaultFileExtension = Path.GetExtension(fileName);
        fileSavePicker.FileTypeChoices.Add("File", [.. new List<string>() { Path.GetExtension(fileName) }]);
        StorageFile? file = await fileSavePicker.PickSaveFileAsync();
        return file?.Path;
    }
}
