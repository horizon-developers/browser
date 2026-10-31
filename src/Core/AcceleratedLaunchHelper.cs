namespace Horizon.Core;

/// <summary>
/// Registers the running instance for accelerated warm launches, so tile and protocol launches are delivered
/// straight to this process as window messages instead of going through a redirecting second instance.
/// This is best-effort, on systems without the feature nothing gets registered and the regular
/// single-instance redirection in <see cref="Program"/> keeps handling those launches.
/// Must be created and disposed on a thread with a message pump (the UI thread).
/// https://learn.microsoft.com/en-us/windows/apps/develop/launch/accelerated-launch
/// </summary>
internal sealed partial class AcceleratedLaunchHelper : IDisposable
{
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("6fb3e48c-ba77-4f80-b9c9-77635a08a7f8")]
    internal partial interface IExperimentalAPIInvoker
    {
        [PreserveSig]
        int IsSupported(string methodName, out int isSupported);

        [PreserveSig]
        int Invoke(string methodName, INamedPropertyStore? args, out int isSupported);
    }

    // https://learn.microsoft.com/en-us/windows/win32/api/propsys/nn-propsys-inamedpropertystore
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
    [Guid("71604b0f-97b0-4764-8577-2f13e98a1422")]
    internal partial interface INamedPropertyStore
    {
        void GetNamedValue(string pszName, out PropVariant ppropvar);

        void SetNamedValue(string pszName, in PropVariant propvar);

        uint GetNameCount();

        nint GetNameAt(uint iProp);
    }

    // Only the members needed for VT_I8, VT_UI4 and VT_VECTOR | VT_LPWSTR, the union starts at offset 8
    // which gives the native size of 24 bytes on 64 bit and 16 bytes on 32 bit
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public PropVariantValue Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariantValue
    {
        [FieldOffset(0)] public long hVal;
        [FieldOffset(0)] public uint ulVal;
        [FieldOffset(0)] public CALPWSTR calpwstr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CALPWSTR
    {
        public uint cElems;
        public nint pElems;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint> lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public nint lpszMenuName;
        public nint lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct COPYDATASTRUCT
    {
        public nuint dwData;
        public uint cbData;
        public nint lpData;
    }

    // Limited Access Feature unlock, bound to the package family name Pinguin2001.HorizonBrowser_m9xs3pkk9sqny
    private const string FeatureId = "com.microsoft.windows.acceleratedlaunch.1";
    private const string FeatureToken = "gyPwVmcn6hnhZk5KwvIa9A==";
    private const string FeatureAttestation = "m9xs3pkk9sqny has registered their use of com.microsoft.windows.acceleratedlaunch.1 with Microsoft and agrees to the terms of use.";

    private const ushort VT_I8 = 20;
    private const ushort VT_UI4 = 19;
    private const ushort VT_LPWSTR = 31;
    private const ushort VT_VECTOR = 0x1000;
    private const uint CLSCTX_INPROC_SERVER = 0x1;
    private const uint WM_COPYDATA = 0x004A;
    // The worker window class is private to this process, so WM_USER can't collide with anything
    private const uint TileLaunchMessageId = 0x0400;
    private const uint CopyDataFormatId = 0;
    private const nint HWND_MESSAGE = -3;
    private const string WorkerClassName = "HorizonAcceleratedLaunchWorker";

    private static readonly Guid CLSID_ExperimentalAPIInvoker = new(0x81AF2611, 0xE262, 0x4090, 0xA1, 0x5B, 0xC0, 0x88, 0x95, 0xB1, 0xA0, 0xF0);
    private static readonly Guid IID_IExperimentalAPIInvoker = new("6fb3e48c-ba77-4f80-b9c9-77635a08a7f8");
    private static readonly Guid IID_INamedPropertyStore = new("71604b0f-97b0-4764-8577-2f13e98a1422");

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid rclsid, nint pUnkOuter, uint dwClsContext, in Guid riid, out IExperimentalAPIInvoker? ppv);

    [LibraryImport("propsys.dll")]
    private static partial int PSCreateMemoryPropertyStore(in Guid riid, out INamedPropertyStore ppv);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    private static partial ushort RegisterClassEx(in WNDCLASSEXW lpwcx);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateWindowEx(uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static partial nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    private static partial nint GetModuleHandle(nint lpModuleName);

    // The worker window proc is a static function pointer, so it routes back to the live helper through this
    private static AcceleratedLaunchHelper? _current;
    private static bool _classRegistered;

    private nint _hwnd;
    // Each Register call works once per invoker, and unregistering has to happen on that same invoker
    private IExperimentalAPIInvoker? _invoker;
    private Action? _tileLaunchCallback;
    private Action<string>? _uriLaunchCallback;
    private string[] _uriSchemes = [];

    /// <summary>
    /// Delivers launches from the app's tile in Start or the taskbar to <paramref name="callback"/>.
    /// </summary>
    public bool TryRegisterForTileLaunch(Action callback)
    {
        if (_tileLaunchCallback != null || !EnsureWorkerWindow())
            return false;

        INamedPropertyStore args = CreatePropertyStore();
        args.SetNamedValue("targetWindow", new PropVariant { vt = VT_I8, Value = new() { hVal = _hwnd } });
        args.SetNamedValue("messageId", new PropVariant { vt = VT_UI4, Value = new() { ulVal = TileLaunchMessageId } });

        int hr = _invoker!.Invoke("RegisterAcceleratedTileLaunch", args, out int isSupported);
        if (hr < 0 || isSupported == 0)
        {
            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"Tile launch registration not accepted, hr: 0x{hr:X8}, isSupported: {isSupported}");
            return false;
        }

        _tileLaunchCallback = callback;
        Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", "Registered for tile launches");
        return true;
    }

    /// <summary>
    /// Delivers protocol launches for <paramref name="schemes"/> to <paramref name="callback"/>.
    /// Only schemes this app is the default handler for are accelerated.
    /// </summary>
    public bool TryRegisterForUris(string[] schemes, Action<string> callback)
    {
        if (_uriLaunchCallback != null || schemes.Length == 0 || !EnsureWorkerWindow())
            return false;

        INamedPropertyStore args = CreatePropertyStore();
        args.SetNamedValue("targetWindow", new PropVariant { vt = VT_I8, Value = new() { hVal = _hwnd } });
        args.SetNamedValue("copyDataFormatId", new PropVariant { vt = VT_UI4, Value = new() { ulVal = CopyDataFormatId } });

        // Kept alive until the registration went through, so it doesn't matter whether the store copies the vector
        nint[] strings = new nint[schemes.Length];
        nint array = Marshal.AllocCoTaskMem(nint.Size * schemes.Length);
        int hr;
        int isSupported;
        try
        {
            for (int i = 0; i < schemes.Length; i++)
            {
                strings[i] = Marshal.StringToCoTaskMemUni(schemes[i]);
                Marshal.WriteIntPtr(array, i * nint.Size, strings[i]);
            }
            args.SetNamedValue("schemes", new PropVariant
            {
                vt = VT_VECTOR | VT_LPWSTR,
                Value = new() { calpwstr = new() { cElems = (uint)schemes.Length, pElems = array } }
            });

            hr = _invoker!.Invoke("RegisterAcceleratedUriLaunch", args, out isSupported);
        }
        finally
        {
            foreach (nint s in strings)
                Marshal.FreeCoTaskMem(s);
            Marshal.FreeCoTaskMem(array);
        }

        if (hr < 0 || isSupported == 0)
        {
            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"URI launch registration not accepted, hr: 0x{hr:X8}, isSupported: {isSupported}");
            return false;
        }

        _uriLaunchCallback = callback;
        _uriSchemes = [.. schemes];
        Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"Registered for URI launches: {string.Join(", ", schemes)}");
        return true;
    }

    public void Dispose()
    {
        if (_hwnd == 0)
            return;

        if (_tileLaunchCallback != null)
            _ = _invoker!.Invoke("UnregisterAcceleratedTileLaunch", null, out _);
        if (_uriLaunchCallback != null)
            _ = _invoker!.Invoke("UnregisterAcceleratedUriLaunch", null, out _);

        _tileLaunchCallback = null;
        _uriLaunchCallback = null;
        _uriSchemes = [];
        _ = DestroyWindow(_hwnd);
        _hwnd = 0;
        _invoker = null;
        if (_current == this)
            _current = null;
    }

    private static bool TryUnlockFeature()
    {
        try
        {
            WAM.LimitedAccessFeatureStatus status = WAM.LimitedAccessFeatures.TryUnlockFeature(FeatureId, FeatureToken, FeatureAttestation).Status;
            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"Feature unlock status: {status}");
            return status is WAM.LimitedAccessFeatureStatus.Available or WAM.LimitedAccessFeatureStatus.AvailableWithoutToken;
        }
        catch
        {
            // Throws without package identity or on systems that predate Limited Access Features
            return false;
        }
    }

    private static INamedPropertyStore CreatePropertyStore()
    {
        Marshal.ThrowExceptionForHR(PSCreateMemoryPropertyStore(IID_INamedPropertyStore, out INamedPropertyStore store));
        return store;
    }

    private unsafe bool EnsureWorkerWindow()
    {
        if (_hwnd != 0)
            return true;

        if (!TryUnlockFeature())
            return false;

        // If the invoker can't be created, the OS doesn't support accelerated launches
        int hr = CoCreateInstance(CLSID_ExperimentalAPIInvoker, 0, CLSCTX_INPROC_SERVER, IID_IExperimentalAPIInvoker, out _invoker);
        if (hr < 0 || _invoker == null)
        {
            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"Not supported on this version of Windows, hr: 0x{hr:X8}");
            _invoker = null;
            return false;
        }

        nint hInstance = GetModuleHandle(0);
        if (!_classRegistered)
        {
            // The class name only has to stay valid during the call, the system keeps its own copy
            fixed (char* className = WorkerClassName)
            {
                WNDCLASSEXW wcex = new()
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    lpfnWndProc = &WorkerWndProc,
                    hInstance = hInstance,
                    lpszClassName = (nint)className
                };
                _classRegistered = RegisterClassEx(wcex) != 0;
            }
        }

        _current = this;
        _hwnd = CreateWindowEx(0, WorkerClassName, null, 0, 0, 0, 0, 0, HWND_MESSAGE, 0, hInstance, 0);
        if (_hwnd == 0)
        {
            Logger.LogEvent(Logger.Severity.Warning, "AcceleratedLaunch", $"Could not create the worker window: {Marshal.GetLastPInvokeError()}");
            _current = null;
            _invoker = null;
            return false;
        }
        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe nint WorkerWndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        AcceleratedLaunchHelper? helper = _current;
        // _hwnd is still 0 while CreateWindowEx sends the creation messages, those go to DefWindowProc
        if (helper != null && hwnd == helper._hwnd)
        {
            try
            {
                switch (msg)
                {
                    case WM_COPYDATA:
                        COPYDATASTRUCT* copyData = (COPYDATASTRUCT*)lParam;
                        if (copyData->dwData == CopyDataFormatId && helper._uriLaunchCallback != null && TryReadUri(copyData, helper._uriSchemes, out string? uri))
                        {
                            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"URI launch from process {wParam}: {uri}");
                            helper._uriLaunchCallback(uri);
                            return 1;
                        }
                        break;
                    case TileLaunchMessageId:
                        if (helper._tileLaunchCallback != null)
                        {
                            Logger.LogEvent(Logger.Severity.Info, "AcceleratedLaunch", $"Tile launch from process {wParam}");
                            helper._tileLaunchCallback();
                            return 0;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // Exceptions must never unwind through native code
                Logger.LogEvent(Logger.Severity.Error, "AcceleratedLaunch", ex.Message);
            }
        }
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    // Any process of the same user can send WM_COPYDATA to this window, so stay within cbData and only accept
    // absolute URIs with one of the registered schemes, like a regular protocol activation would deliver
    private static unsafe bool TryReadUri(COPYDATASTRUCT* copyData, string[] schemes, [NotNullWhen(true)] out string? uri)
    {
        uri = null;
        if (copyData->lpData == 0 || copyData->cbData < sizeof(char))
            return false;

        // Copied out right away, the buffer is only valid while handling the message
        ReadOnlySpan<char> data = new((void*)copyData->lpData, (int)(copyData->cbData / sizeof(char)));
        int terminator = data.IndexOf('\0');
        string text = (terminator >= 0 ? data[..terminator] : data).ToString();

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? parsed) || !schemes.Contains(parsed.Scheme, StringComparer.OrdinalIgnoreCase))
            return false;

        uri = text;
        return true;
    }
}
