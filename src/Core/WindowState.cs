namespace Horizon.Core;

[JsonSerializable(typeof(WindowState))]
public partial class WindowStateSerializerContext : JsonSerializerContext
{
}

public class WindowState
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 920;
    public int Height { get; set; } = 720;
    public bool IsMaximized { get; set; } = false;
}
