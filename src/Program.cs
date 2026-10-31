namespace Horizon;

public static class Program
{
    [STAThread]
    [SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "Required for app init")]
    static void Main()
    {
        bool Redirect = DecideRedirection();

        if (!Redirect)
        {
            Application.Start(_ => new App());
        }
    }
    static bool DecideRedirection()
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("HorizonSingleInstance");

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            _ = Windows.Win32.PInvoke.AllowSetForegroundWindow(mainInstance.ProcessId);
            // Redirect activation to that instance
            mainInstance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs()).AsTask().Wait();

            // And exit our instance and stop
            Environment.Exit(0);
            return true;
        }
        return false;
    }
}
