namespace Horizon.Core;

public class FileHelper
{
    public static async Task SaveBytesAsFileAsync(string fileName, byte[] buffer, string filetypefriendlyname, string filetype) => await SaveFileAsync(fileName, filetypefriendlyname, filetype, buffer, null);

    public static async Task SaveStringAsFileAsync(string fileName, string fileContent, string filetypefriendlyname, string filetype) => await SaveFileAsync(fileName, filetypefriendlyname, filetype, null, fileContent);

    private static async Task SaveFileAsync(string fileName, string filetypefriendlyname, string filetype, byte[] BytesFileContent = null, string TextFileContent = null)
    {
        // Create a file picker
        FileSavePicker savePicker = new()
        {
            // Set options for your file picker
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        // See the sample code below for how to make the window accessible from the App class.
        var window = WindowHelper.MainWindow;

        // Initialize the file picker with the window handle (HWND).
        InitializeWithWindow.Initialize(savePicker, WindowHelper.HWND);

        // Dropdown of file types the user can save the file as
        savePicker.FileTypeChoices.Add(filetypefriendlyname, new List<string>() { filetype });
        // Default file name if the user does not type one in or select a file to replace
        savePicker.SuggestedFileName = fileName;

        // Open the picker for the user to pick a file
        StorageFile file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
            CachedFileManager.DeferUpdates(file);

            // write to file
            // depending on which type we either write bytes or text
            if (BytesFileContent != null)
            {
                await FileIO.WriteBytesAsync(file, BytesFileContent);
            }
            if (TextFileContent != null)
            {
                await FileIO.WriteTextAsync(file, TextFileContent);
            }

            // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
            if (status == FileUpdateStatus.Complete)
            {
                //NotificationHelper.NotifyUser("Success", "File " + file.Name + " was saved to\n" + file.Path);
            }
            else
            {
                //await UI.ShowDialog("Error", "File " + file.Name + " couldn't be saved.");
            }
        }
    }

    public static async Task DeleteLocalFile(string fileName)
    {
        var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
        if (file != null)
            await file.DeleteAsync();
    }
}