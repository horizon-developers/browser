namespace Horizon.Core;

class ReleaseNotesNotifier
{
    private void HandleFirstStartupAfterUpdate()
    {
        string LastUsedBuild = SettingsHelper.GetSetting("LastUsedBuild");
        int CurrentBuild = AppVersionHelper.GetAppBuild();
        SettingsHelper.SetSetting("LastUsedBuild", CurrentBuild.ToString());

        if (string.IsNullOrEmpty(LastUsedBuild))
        {
            //ShowUpdatedDialog();
            return;
        }

        int LastUsedBuildAsInt = int.Parse(LastUsedBuild);

        if (CurrentBuild > LastUsedBuildAsInt)
        {
            //ShowUpdatedDialog();
            return;
        }
    }

    private async void ShowUpdatedDialog(XamlRoot root)
    {
        ContentDialog dialog = new()
        {
            Title = "Horizon has just been updated!",
            CloseButtonText = "Got it",
            PrimaryButtonText = "What's new?",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            WindowHelper.MainWindow.CreateTab("Release notes", typeof(WhatsNewPage));
        }
    }
}
