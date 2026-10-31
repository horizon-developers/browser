namespace Horizon;

public static class Program
{
    [STAThread]
    static void Main()
    {
        bool Redirect = DecideRedirection();

        if (!Redirect)
        {
            ComWrappersSupport.InitializeComWrappers();
            Application.Start(static p =>
            {
                // Without this, awaits on the UI thread resume on the thread pool, where WebView2 and XAML objects can't be touched
                System.Threading.SynchronizationContext.SetSynchronizationContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()));
                _ = new App();
            });
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
