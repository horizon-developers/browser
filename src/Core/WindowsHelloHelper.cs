namespace Horizon.Core;

public class WindowsHelloHelper
{
    public static async Task<bool> CheckSec()
    {
        if (await CheckAvailability() == UserConsentVerifierAvailability.Available)
        {
            UserConsentVerificationResult consent = await UserConsentVerifier.RequestVerificationAsync(string.Empty);
            if (consent == UserConsentVerificationResult.Verified)
            {
                return true;
            }
            return false;
        }
        return true;
    }

    public static async Task<UserConsentVerifierAvailability> CheckAvailability()
    {
        UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
        return availability;
    }

}
