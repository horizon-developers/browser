namespace Horizon.Core;

public static class AppVersionHelper
{
    public static string GetAppVersion()
    {
        WAM.Package package = WAM.Package.Current;
        WAM.PackageId packageId = package.Id;
        WAM.PackageVersion version = packageId.Version;

        return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
    }
}
