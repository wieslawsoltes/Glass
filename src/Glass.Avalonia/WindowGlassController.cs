using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Glass.Avalonia;

internal sealed class WindowGlassController : IDisposable
{
    private readonly IWindowGlassBackend _backend;
    private readonly Window _window;
    private bool _disposed;
    private WindowGlassSettings _settings = new();

    public WindowGlassController(Window window)
    {
        _window = window;
        _backend = WindowGlassBackendFactory.Create();
        _window.Opened += OnWindowOpened;
        _window.Closed += OnWindowClosed;
        _window.PropertyChanged += OnWindowPropertyChanged;
    }

    public void Apply(WindowGlassSettings settings)
    {
        _settings = settings;
        ConfigureAvaloniaSurface();
        ApplyNativeIfPossible();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _window.Opened -= OnWindowOpened;
        _window.Closed -= OnWindowClosed;
        _window.PropertyChanged -= OnWindowPropertyChanged;
        _backend.Reset(_window);
    }

    private void ConfigureAvaloniaSurface()
    {
        _window.Background = Brushes.Transparent;
        _window.TransparencyBackgroundFallback = new SolidColorBrush(Color.FromArgb(0x08, 0x09, 0x0C, 0x10));
        _window.TransparencyLevelHint = new WindowTransparencyLevelCollection(
            new[]
            {
                PreferredTransparency(_settings.Material),
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.Transparent
            }.Distinct().ToList());
    }

    private void ApplyNativeIfPossible()
    {
        if (_window.PlatformImpl is null)
        {
            return;
        }

        _backend.Apply(_window, _settings);
    }

    private static WindowTransparencyLevel PreferredTransparency(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Mica or GlassMaterial.MicaAlt or GlassMaterial.LiquidGlass => WindowTransparencyLevel.Mica,
            GlassMaterial.Acrylic => WindowTransparencyLevel.AcrylicBlur,
            _ => WindowTransparencyLevel.Blur
        };

    private void OnWindowOpened(object? sender, EventArgs e) => ApplyNativeIfPossible();

    private void OnWindowClosed(object? sender, EventArgs e) => Dispose();

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ActualThemeVariantProperty)
        {
            ApplyNativeIfPossible();
        }
    }
}
