using Avalonia.Controls;

namespace Glass.Avalonia;

internal interface IWindowGlassBackend
{
    void Apply(Window window, WindowGlassSettings settings);

    void Reset(Window window);
}

internal static class WindowGlassBackendFactory
{
    public static IWindowGlassBackend Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsWindowGlassBackend();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacWindowGlassBackend();
        }

        return new NoopWindowGlassBackend();
    }
}

internal sealed class NoopWindowGlassBackend : IWindowGlassBackend
{
    public void Apply(Window window, WindowGlassSettings settings)
    {
    }

    public void Reset(Window window)
    {
    }
}
