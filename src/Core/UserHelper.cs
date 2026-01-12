namespace Horizon.Core;

public static class UserHelper
{
    private static WS.User CurrentUser { get; set; }

    public static async Task<BitmapImage> GetUserPicture()
    {
        try
        {
            if (CurrentUser == null)
            {
                CurrentUser = WS.User.GetDefault();
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
#if DEBUG
                    Logger.LogEvent(Logger.Severity.Error, "User (AccountPicture)", $"Error loading picture: {ex.Message}");
#endif
                }
            }
            return bitmapImage;

        }
        catch (Exception ex)
        {
#if DEBUG
            Logger.LogEvent(Logger.Severity.Error, "User (AccountPicture)", $"Error loading picture: {ex.Message}");
#endif
            return null;
        }
    }

    public static async Task<string> GetUserName()
    {
        return Environment.UserName;
    }
}
