namespace Horizon.Core;

/// <summary>
/// A helper class for common Windows Hello functionality
/// </summary>
public class WindowsHelloHelper
{
    /// <summary>
    /// Performs an authentication using Windows Hello
    /// </summary>
    /// <returns>If the authentication has been successful</returns>
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

    /// <summary>
    /// Checks if Windows Hello is currently available
    /// </summary>
    /// <returns>A UserConsentVerifierAvailability value that describes the result of the availability check operation.</returns>
    public static async Task<UserConsentVerifierAvailability> CheckAvailability()
    {
        UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
        return availability;
    }
}
