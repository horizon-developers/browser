namespace Horizon.Core;

public static class UserHelper
{
    private static WS.User? CurrentUser { get; set; }
    private static BitmapImage? CurrentUserPicture { get; set; }

    public static async Task<BitmapImage?> GetUserPicture()
    {
        // User.GetDefault only exists since Windows 11 / Server 2022 (build 20348)
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
        {
            return null;
        }
        try
        {
            if (CurrentUser == null)
            {
                CurrentUser = WS.User.GetDefault();
            }
            if (CurrentUserPicture != null)
            {
                return CurrentUserPicture;
            }

            IRandomAccessStreamReference pictureReference = await CurrentUser.GetPictureAsync(WS.UserPictureSize.Size208x208);

            BitmapImage bitmapImage = new();

            if (pictureReference != null)
            {
                try
                {
                    using (IRandomAccessStream stream = await pictureReference.OpenReadAsync())
                    {

                        await bitmapImage.SetSourceAsync(stream);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogEvent(Logger.Severity.Error, "User (AccountPicture)", $"Error loading picture: {ex.Message}");
                }
            }
            CurrentUserPicture = bitmapImage;
            return bitmapImage;

        }
        catch (Exception ex)
        {
            Logger.LogEvent(Logger.Severity.Error, "User (AccountPicture)", $"Error loading picture: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> GetUserName()
    {
        return Environment.UserName;
    }
}
