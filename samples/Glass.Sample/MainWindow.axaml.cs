using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Glass.Avalonia;

namespace Glass.Sample;

public partial class MainWindow : Window
{
    private static readonly IReadOnlyDictionary<GlassMaterial, Preset> Presets = new Dictionary<GlassMaterial, Preset>
    {
        [GlassMaterial.LiquidGlass] = new(
            "Liquid Glass",
            "AppKit liquid glass where the host exposes it, otherwise bright vibrancy with a tight white tint.",
            Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)),
        [GlassMaterial.MicaAlt] = new(
            "Mica Alt",
            "A calmer long-lived backdrop with slightly denser diffusion and a cooler blue edge.",
            Color.FromArgb(0x40, 0xD6, 0xE6, 0xFF)),
        [GlassMaterial.Acrylic] = new(
            "Acrylic",
            "A brighter transient glass mode that leans into separation and contrast.",
            Color.FromArgb(0x4A, 0xC8, 0xFF, 0xF1)),
        [GlassMaterial.Mica] = new(
            "Mica",
            "A more grounded surface that keeps depth without the sharpest highlight treatment.",
            Color.FromArgb(0x34, 0xF1, 0xF5, 0xFF))
    };

    public MainWindow()
    {
        InitializeComponent();
        CurrentPlatformText.Text = OperatingSystem.IsMacOS() ? "macOS host" : OperatingSystem.IsWindows() ? "Windows host" : "Fallback host";
        ApplyPreset(GlassMaterial.LiquidGlass);
    }

    private void OnPresetClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tag } && Enum.TryParse<GlassMaterial>(tag, out var material))
        {
            ApplyPreset(material);
        }
    }

    private void ApplyPreset(GlassMaterial material)
    {
        var preset = Presets[material];
        WindowGlass.Apply(
            this,
            new WindowGlassSettings
            {
                Material = material,
                TintColor = preset.TintColor,
                CornerRadius = 30
            });

        CurrentMaterialText.Text = preset.Title;
        CurrentPresetTitle.Text = preset.Title;
        CurrentMaterialDescription.Text = preset.Description;
        CurrentBackendText.Text = BuildBackendLabel(material);
    }

    private static string BuildBackendLabel(GlassMaterial material)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
            {
                return "Avalonia fallback";
            }

            return material switch
            {
                GlassMaterial.Acrylic or GlassMaterial.Blur => "DWM Acrylic",
                GlassMaterial.Mica => "DWM Mica",
                _ => "DWM Mica Alt"
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return material == GlassMaterial.LiquidGlass
                ? "AppKit liquid/vibrancy"
                : "AppKit vibrancy";
        }

        return "Transparent shell";
    }

    private readonly record struct Preset(string Title, string Description, Color TintColor);
}
