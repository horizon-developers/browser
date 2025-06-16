namespace Horizon.Core;

public class WebView2Error(string ErrorMsg)
{
    public string ErrorMsg { get; private set; } = ErrorMsg;
}