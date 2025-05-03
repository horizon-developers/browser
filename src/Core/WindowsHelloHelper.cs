namespace Horizon.Core;

internal class WindowsHelloHelper
{
    private static async Task<bool> CheckSec()
    {
        UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
        if (availability == UserConsentVerifierAvailability.Available)
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
}
