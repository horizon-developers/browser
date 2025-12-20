namespace Horizon.Core;

/// <summary>
/// A helper for creating the ShareUI on the main window.
/// </summary>
public static partial class ShareHelper
{
    // https://learn.microsoft.com/en-us/windows/apps/develop/ui/display-ui-objects#for-classes-that-implement-idatatransfermanagerinterop
    [GeneratedComInterface]
    [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow(IntPtr appWindow, ref Guid riid);

        void ShowShareUIForWindow(IntPtr appWindow);
    }

    internal static Guid _dtm_iid =
        new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

    /// <summary>
    /// Initializes and shows the ShareUI on the main window.
    /// </summary>
    public static void Share(string title, Uri url)
    {
        var dataTransferManagerFactory = DataTransferManager.As<IDataTransferManagerInterop>();

        IntPtr result = dataTransferManagerFactory.GetForWindow(WindowHelper.HWND, ref _dtm_iid);

        var dataTransferManager = MarshalInterface<DataTransferManager>.FromAbi(result);

        dataTransferManager.DataRequested += (sender, args) =>
        {
            args.Request.Data.Properties.Title = title;
            args.Request.Data.SetUri(url);
            args.Request.Data.RequestedOperation = DataPackageOperation.Link;
        };

        dataTransferManagerFactory.ShowShareUIForWindow(WindowHelper.HWND);
    }
}