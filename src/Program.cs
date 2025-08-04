namespace Horizon;

public static class Program
{
#pragma warning disable IDE0060
#pragma warning disable IDE0079
#pragma warning disable CA1806
    [STAThread]
    static void Main(string[] args)
    {
        bool Redirect = DecideRedirection();

        if (!Redirect)
        {
            ComWrappersSupport.InitializeComWrappers();
            Application.Start((p) => {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
    }
#pragma warning restore IDE0060
#pragma warning restore CA1806
#pragma warning restore IDE0079
    static bool DecideRedirection()
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey("HorizonSingleInstance");

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            // Redirect activation to that instance
            mainInstance.RedirectActivationToAsync(appArgs).AsTask().Wait();

            // And exit our instance and stop
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return true;
        }
        return false;
    }
}
