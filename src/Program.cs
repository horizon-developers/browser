namespace Horizon;

public static class Program
{
#pragma warning disable IDE0060
    [STAThread]
    static void Main(string[] args)
    {
        bool Redirect = DecideRedirection();

        if (!Redirect)
        {
            ComWrappersSupport.InitializeComWrappers();
            Application.Start(static (p) => {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
    }
#pragma warning restore IDE0060
    static bool DecideRedirection()
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("HorizonSingleInstance");

        // If the main instance isn't this current instance
        if (!mainInstance.IsCurrent)
        {
            // Redirect activation to that instance
            mainInstance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs()).AsTask().Wait();

            // And exit our instance and stop
            Environment.Exit(0);
            return true;
        }
        return false;
    }
}
