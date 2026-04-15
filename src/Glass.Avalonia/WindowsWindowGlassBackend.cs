using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Glass.Avalonia;

internal sealed partial class WindowsWindowGlassBackend : IWindowGlassBackend
{
    private const int DwmUseHostBackdropBrush = 17;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmSystemBackdropType = 38;

    public void Apply(Window window, WindowGlassSettings settings)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        SetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, window.ActualThemeVariant == ThemeVariant.Dark ? 1 : 0);
        SetWindowAttribute(hwnd, DwmWindowCornerPreference, settings.CornerRadius >= 24 ? (uint)DwmWindowCornerPreferenceValue.Round : (uint)DwmWindowCornerPreferenceValue.Default);

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            SetWindowAttribute(hwnd, DwmUseHostBackdropBrush, settings.Material is GlassMaterial.Acrylic or GlassMaterial.Blur or GlassMaterial.LiquidGlass ? 1 : 0);
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            SetWindowAttribute(hwnd, DwmSystemBackdropType, (uint)MapBackdrop(settings.Material));
        }
    }

    public void Reset(Window window)
    {
    }

    private static DwmSystemBackdropTypeValue MapBackdrop(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Mica => DwmSystemBackdropTypeValue.MainWindow,
            GlassMaterial.MicaAlt or GlassMaterial.LiquidGlass => DwmSystemBackdropTypeValue.TabbedWindow,
            GlassMaterial.Acrylic or GlassMaterial.Blur => DwmSystemBackdropTypeValue.TransientWindow,
            _ => DwmSystemBackdropTypeValue.Auto
        };

    private static unsafe void SetWindowAttribute<T>(IntPtr hwnd, int attribute, T value)
        where T : unmanaged
    {
        _ = DwmSetWindowAttribute(hwnd, attribute, &value, sizeof(T));
    }

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
    private static unsafe partial int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, void* pvAttribute, int cbAttribute);

    private enum DwmWindowCornerPreferenceValue : uint
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    private enum DwmSystemBackdropTypeValue : uint
    {
        Auto = 0,
        None = 1,
        MainWindow = 2,
        TransientWindow = 3,
        TabbedWindow = 4
    }
}
