using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Glass.Avalonia;

public sealed class WindowGlass : AvaloniaObject
{
    private static readonly ConditionalWeakTable<Window, WindowGlassController> Controllers = new();

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, bool>("IsEnabled");

    public static readonly AttachedProperty<GlassMaterial> MaterialProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, GlassMaterial>("Material", GlassMaterial.LiquidGlass);

    public static readonly AttachedProperty<Color> TintColorProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, Color>("TintColor", Color.FromArgb(0x48, 0xFF, 0xFF, 0xFF));

    public static readonly AttachedProperty<double> CornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, double>("CornerRadius", 28d);

    public static readonly AttachedProperty<WindowGlassWindowsSettings> WindowsOptionsProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, WindowGlassWindowsSettings>("WindowsOptions", new WindowGlassWindowsSettings());

    public static readonly AttachedProperty<WindowGlassMacSettings> MacOptionsProperty =
        AvaloniaProperty.RegisterAttached<WindowGlass, Window, WindowGlassMacSettings>("MacOptions", new WindowGlassMacSettings());

    static WindowGlass()
    {
        IsEnabledProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
        MaterialProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
        TintColorProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
        CornerRadiusProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
        WindowsOptionsProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
        MacOptionsProperty.Changed.AddClassHandler<Window>(OnSettingsChanged);
    }

    public static bool GetIsEnabled(Window window) => window.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Window window, bool value) => window.SetValue(IsEnabledProperty, value);

    public static GlassMaterial GetMaterial(Window window) => window.GetValue(MaterialProperty);

    public static void SetMaterial(Window window, GlassMaterial value) => window.SetValue(MaterialProperty, value);

    public static Color GetTintColor(Window window) => window.GetValue(TintColorProperty);

    public static void SetTintColor(Window window, Color value) => window.SetValue(TintColorProperty, value);

    public static double GetCornerRadius(Window window) => window.GetValue(CornerRadiusProperty);

    public static void SetCornerRadius(Window window, double value) => window.SetValue(CornerRadiusProperty, value);

    public static WindowGlassWindowsSettings GetWindowsOptions(Window window) => window.GetValue(WindowsOptionsProperty);

    public static void SetWindowsOptions(Window window, WindowGlassWindowsSettings value) => window.SetValue(WindowsOptionsProperty, value);

    public static WindowGlassMacSettings GetMacOptions(Window window) => window.GetValue(MacOptionsProperty);

    public static void SetMacOptions(Window window, WindowGlassMacSettings value) => window.SetValue(MacOptionsProperty, value);

    public static void Apply(Window window, WindowGlassSettings settings)
    {
        SetIsEnabled(window, true);
        SetMaterial(window, settings.Material);
        SetTintColor(window, settings.TintColor);
        SetCornerRadius(window, settings.CornerRadius);
        SetWindowsOptions(window, settings.Windows);
        SetMacOptions(window, settings.Mac);
    }

    private static void OnSettingsChanged(Window window, AvaloniaPropertyChangedEventArgs _)
    {
        if (!GetIsEnabled(window))
        {
            if (Controllers.TryGetValue(window, out var existing))
            {
                existing.Dispose();
                Controllers.Remove(window);
            }

            return;
        }

        var controller = Controllers.GetValue(window, static host => new WindowGlassController(host));
        controller.Apply(BuildSettings(window));
    }

    private static WindowGlassSettings BuildSettings(Window window) =>
        new()
        {
            Material = GetMaterial(window),
            TintColor = GetTintColor(window),
            CornerRadius = Math.Clamp(GetCornerRadius(window), 0, 56),
            Windows = GetWindowsOptions(window),
            Mac = GetMacOptions(window)
        };
}
