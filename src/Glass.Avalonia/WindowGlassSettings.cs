using Avalonia.Media;

namespace Glass.Avalonia;

public sealed record class WindowGlassSettings
{
    public GlassMaterial Material { get; init; } = GlassMaterial.LiquidGlass;

    public Color TintColor { get; init; } = Color.FromArgb(0x48, 0xFF, 0xFF, 0xFF);

    public double CornerRadius { get; init; } = 28;

    public WindowGlassWindowsSettings Windows { get; init; } = new();

    public WindowGlassMacSettings Mac { get; init; } = new();
}
