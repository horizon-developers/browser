#if DEBUG
using System.Diagnostics;

namespace Horizon.Core;

internal static class Logger
{
    internal static void LogEvent(Severity severity, string source, string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string level = severity.ToString().ToUpperInvariant();

        string formattedMessage = $"[{timestamp}] {level} {source}: {message}";

        Debug.WriteLine(formattedMessage);
    }

    internal enum Severity
    {
        Info,
        Warning,
        Error
    }
}
#endif