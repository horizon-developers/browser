using System.Text;

namespace Horizon.Core;
public static class FileNameHelper
{
    /// <summary>
    /// Normalizes and sanitizes a string into a valid Windows filename.
    /// </summary>
    public static string ToValidFileName(string input, string replacement = "_")
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unnamed";

        string name = input.Normalize(NormalizationForm.FormC);

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), replacement);
        }

        string[] reserved =
        {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        if (reserved.Contains(name.ToUpperInvariant()))
            name = "_" + name;

        // Windows does not allow filenames ending with space or dot
        name = name.TrimEnd(' ', '.');

        // If empty after sanitization
        if (string.IsNullOrEmpty(name))
            name = "Untitled";

        // Enforce max length of 50 characters
        if (name.Length > 50)
            name = name[..50];

        return name;
    }
}
