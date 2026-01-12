namespace Horizon.Core;

public class FileHelper
{
    public static async Task<string> SaveBytesAsFileAsync(string fileName, byte[] buffer, string filetypefriendlyname, string filetype) => await SaveFileAsync(fileName, filetypefriendlyname, filetype, buffer, null);

    public static async Task SaveStringAsFileAsync(string fileName, string fileContent, string filetypefriendlyname, string filetype) => await SaveFileAsync(fileName, filetypefriendlyname, filetype, null, fileContent);

    private static async Task<string> SaveFileAsync(string fileName, string filetypefriendlyname, string filetype, byte[] BytesFileContent = null, string TextFileContent = null)
    {
        // Create a file picker
        FileSavePicker savePicker = new(WindowHelper.MainWindow.AppWindow.Id)
        {
            // Set options for your file picker
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };

        // Dropdown of file types the user can save the file as
        savePicker.FileTypeChoices.Add(filetypefriendlyname, new List<string>() { filetype });
        // Default file name if the user does not type one in or select a file to replace
        savePicker.SuggestedFileName = fileName;

        // Open the picker for the user to pick a file
        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            if (BytesFileContent != null)
            {
                await File.WriteAllBytesAsync(file.Path, BytesFileContent);
            }
            if (TextFileContent != null)
            {
                await File.WriteAllTextAsync(file.Path, TextFileContent);
            }
        }
        if (file.Path != null) {
            return file.Path;
        }
        return "Unknown path";
    }

    public static async Task DeleteLocalFile(string fileName)
    {
        var file = await FolderHelper.LocalFolder.TryGetItemAsync(fileName);
        if (file != null)
            await file.DeleteAsync();
    }
}