using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Glass.Avalonia;

internal sealed partial class WindowsWindowGlassBackend : IWindowGlassBackend
{
    private const int DwmUseHostBackdropBrush = 17;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmWindowCornerPreference = 33;
    private const int DwmBorderColor = 34;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;
    private const int DwmSystemBackdropType = 38;
    private const uint DwmColorDefault = 0xFFFFFFFF;
    private const uint DwmColorNone = 0xFFFFFFFE;

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

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            SetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ResolveImmersiveDarkMode(window, settings) ? 1 : 0);
            SetWindowAttribute(hwnd, DwmWindowCornerPreference, (uint)ResolveCornerPreference(settings));
            SetWindowAttribute(hwnd, DwmUseHostBackdropBrush, ResolveHostBackdropBrush(settings) ? 1 : 0);
            SetWindowAttribute(hwnd, DwmBorderColor, ResolveBorderColor(settings));
            SetWindowAttribute(hwnd, DwmCaptionColor, ResolveCaptionColor(settings));
            SetWindowAttribute(hwnd, DwmTextColor, ResolveTextColor(settings));
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            SetWindowAttribute(hwnd, DwmSystemBackdropType, (uint)ResolveBackdrop(settings));
        }
    }

    public void Reset(Window window)
    {
    }

    private static DwmSystemBackdropTypeValue ResolveBackdrop(WindowGlassSettings settings) =>
        settings.Windows.BackdropKind switch
        {
            WindowGlassWindowsBackdropKind.Auto => DwmSystemBackdropTypeValue.Auto,
            WindowGlassWindowsBackdropKind.None => DwmSystemBackdropTypeValue.None,
            WindowGlassWindowsBackdropKind.MainWindow => DwmSystemBackdropTypeValue.MainWindow,
            WindowGlassWindowsBackdropKind.TransientWindow => DwmSystemBackdropTypeValue.TransientWindow,
            WindowGlassWindowsBackdropKind.TabbedWindow => DwmSystemBackdropTypeValue.TabbedWindow,
            _ => MapBackdrop(settings.Material)
        };

    private static DwmSystemBackdropTypeValue MapBackdrop(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Mica => DwmSystemBackdropTypeValue.MainWindow,
            GlassMaterial.MicaAlt or GlassMaterial.LiquidGlass => DwmSystemBackdropTypeValue.TabbedWindow,
            GlassMaterial.Acrylic or GlassMaterial.Blur => DwmSystemBackdropTypeValue.TransientWindow,
            _ => DwmSystemBackdropTypeValue.Auto
        };

    private static bool ResolveImmersiveDarkMode(Window window, WindowGlassSettings settings) =>
        settings.Windows.ImmersiveDarkMode switch
        {
            WindowGlassWindowsSwitch.Off => false,
            WindowGlassWindowsSwitch.On => true,
            _ => window.ActualThemeVariant == ThemeVariant.Dark
        };

    private static bool ResolveHostBackdropBrush(WindowGlassSettings settings) =>
        settings.Windows.HostBackdropBrush switch
        {
            WindowGlassWindowsSwitch.Off => false,
            WindowGlassWindowsSwitch.On => true,
            _ => settings.Material is GlassMaterial.Acrylic or GlassMaterial.Blur or GlassMaterial.LiquidGlass
        };

    private static DwmWindowCornerPreferenceValue ResolveCornerPreference(WindowGlassSettings settings) =>
        settings.Windows.CornerKind switch
        {
            WindowGlassWindowsCornerKind.Default => DwmWindowCornerPreferenceValue.Default,
            WindowGlassWindowsCornerKind.DoNotRound => DwmWindowCornerPreferenceValue.DoNotRound,
            WindowGlassWindowsCornerKind.Round => DwmWindowCornerPreferenceValue.Round,
            WindowGlassWindowsCornerKind.RoundSmall => DwmWindowCornerPreferenceValue.RoundSmall,
            _ => settings.CornerRadius >= 24 ? DwmWindowCornerPreferenceValue.Round : DwmWindowCornerPreferenceValue.Default
        };

    private static uint ResolveBorderColor(WindowGlassSettings settings) =>
        settings.Windows.BorderColorMode switch
        {
            WindowGlassWindowsBorderColorMode.Custom => ToColorRef(settings.Windows.BorderColor),
            WindowGlassWindowsBorderColorMode.None => DwmColorNone,
            _ => DwmColorDefault
        };

    private static uint ResolveCaptionColor(WindowGlassSettings settings) =>
        settings.Windows.CaptionColorMode == WindowGlassWindowsColorMode.Custom
            ? ToColorRef(settings.Windows.CaptionColor)
            : DwmColorDefault;

    private static uint ResolveTextColor(WindowGlassSettings settings) =>
        settings.Windows.TextColorMode == WindowGlassWindowsColorMode.Custom
            ? ToColorRef(settings.Windows.TextColor)
            : DwmColorDefault;

    private static uint ToColorRef(global::Avalonia.Media.Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

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
