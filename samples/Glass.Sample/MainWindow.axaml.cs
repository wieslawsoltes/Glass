using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Glass.Avalonia;

namespace Glass.Sample;

public partial class MainWindow : Window
{
    private static readonly IReadOnlyDictionary<GlassMaterial, Preset> Presets = new Dictionary<GlassMaterial, Preset>
    {
        [GlassMaterial.Auto] = new(
            "System Auto",
            "Lets each host choose its preferred window material while keeping the Avalonia shell transparent.",
            Color.FromArgb(0x42, 0xF4, 0xF8, 0xFF)),
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
            Color.FromArgb(0x34, 0xF1, 0xF5, 0xFF)),
        [GlassMaterial.Blur] = new(
            "Blur",
            "A neutral blur-first preset for hosts that support a simpler translucent treatment.",
            Color.FromArgb(0x36, 0xFF, 0xFF, 0xFF))
    };

    private bool _updatingControls;
    private WindowGlassSettings _settings = CreatePresetSettings(GlassMaterial.LiquidGlass);

    public MainWindow()
    {
        InitializeComponent();
        ConfigureChrome();
        InitializeControlPanel();
        CurrentPlatformText.Text = OperatingSystem.IsMacOS() ? "macOS host" : OperatingSystem.IsWindows() ? "Windows host" : "Fallback host";
        ApplyPreset(GlassMaterial.LiquidGlass);
    }

    private void ConfigureChrome()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        SystemDecorations = SystemDecorations.None;
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        ExtendClientAreaTitleBarHeightHint = 76;
        MacTrafficLights.IsVisible = true;
    }

    private void InitializeControlPanel()
    {
        MaterialComboBox.ItemsSource = Enum.GetValues<GlassMaterial>();

        WindowsBackdropComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsBackdropKind>();
        WindowsCornerComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsCornerKind>();
        WindowsHostBackdropComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsSwitch>();
        WindowsDarkModeComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsSwitch>();
        WindowsBorderModeComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsBorderColorMode>();
        WindowsCaptionModeComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsColorMode>();
        WindowsTextModeComboBox.ItemsSource = Enum.GetValues<WindowGlassWindowsColorMode>();

        MacMaterialComboBox.ItemsSource = Enum.GetValues<WindowGlassMacMaterialKind>();
        MacBlendingComboBox.ItemsSource = Enum.GetValues<WindowGlassMacBlendingKind>();
        MacStateComboBox.ItemsSource = Enum.GetValues<WindowGlassMacStateKind>();
        MacEmphasisComboBox.ItemsSource = Enum.GetValues<WindowGlassMacEmphasisKind>();
        MacGlassStyleComboBox.ItemsSource = Enum.GetValues<WindowGlassMacGlassStyleKind>();

        WindowsOptionsPanel.IsVisible = OperatingSystem.IsWindows();
        MacOptionsPanel.IsVisible = OperatingSystem.IsMacOS();
        UnsupportedOptionsText.IsVisible = !OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS();

        if (OperatingSystem.IsWindows())
        {
            WindowsBackdropComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);
            WindowsHostBackdropComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            WindowsDarkModeComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            WindowsCornerComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            WindowsBorderModeComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            WindowsCaptionModeComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            WindowsTextModeComboBox.IsEnabled = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
        }
    }

    private static WindowGlassSettings CreatePresetSettings(GlassMaterial material)
    {
        var preset = LookupPreset(material);
        return new WindowGlassSettings
        {
            Material = material,
            TintColor = preset.TintColor,
            CornerRadius = 30,
            Windows = new WindowGlassWindowsSettings(),
            Mac = new WindowGlassMacSettings()
        };
    }

    private static Preset LookupPreset(GlassMaterial material) =>
        Presets.TryGetValue(material, out var preset)
            ? preset
            : Presets[GlassMaterial.LiquidGlass];

    private void ApplyPreset(GlassMaterial material)
    {
        _settings = CreatePresetSettings(material);
        ApplySettings();
    }

    private void ApplySettings()
    {
        var preset = LookupPreset(_settings.Material);
        WindowGlass.Apply(this, _settings);

        CurrentMaterialText.Text = preset.Title;
        CurrentPresetTitle.Text = preset.Title;
        CurrentMaterialDescription.Text = preset.Description;
        CurrentBackendText.Text = BuildBackendLabel(_settings);

        UpdateControlValues();
    }

    private void UpdateControlValues()
    {
        _updatingControls = true;

        MaterialComboBox.SelectedItem = _settings.Material;
        CornerRadiusSlider.Value = _settings.CornerRadius;
        CornerRadiusValueText.Text = $"{Math.Round(_settings.CornerRadius):0}px";
        TintColorTextBox.Text = FormatColor(_settings.TintColor);

        WindowsBackdropComboBox.SelectedItem = _settings.Windows.BackdropKind;
        WindowsCornerComboBox.SelectedItem = _settings.Windows.CornerKind;
        WindowsHostBackdropComboBox.SelectedItem = _settings.Windows.HostBackdropBrush;
        WindowsDarkModeComboBox.SelectedItem = _settings.Windows.ImmersiveDarkMode;
        WindowsBorderModeComboBox.SelectedItem = _settings.Windows.BorderColorMode;
        WindowsBorderColorTextBox.Text = FormatColor(_settings.Windows.BorderColor);
        WindowsBorderColorTextBox.IsEnabled = _settings.Windows.BorderColorMode == WindowGlassWindowsBorderColorMode.Custom;
        WindowsCaptionModeComboBox.SelectedItem = _settings.Windows.CaptionColorMode;
        WindowsCaptionColorTextBox.Text = FormatColor(_settings.Windows.CaptionColor);
        WindowsCaptionColorTextBox.IsEnabled = _settings.Windows.CaptionColorMode == WindowGlassWindowsColorMode.Custom;
        WindowsTextModeComboBox.SelectedItem = _settings.Windows.TextColorMode;
        WindowsTextColorTextBox.Text = FormatColor(_settings.Windows.TextColor);
        WindowsTextColorTextBox.IsEnabled = _settings.Windows.TextColorMode == WindowGlassWindowsColorMode.Custom;

        MacMaterialComboBox.SelectedItem = _settings.Mac.VisualMaterial;
        MacBlendingComboBox.SelectedItem = _settings.Mac.BlendingKind;
        MacStateComboBox.SelectedItem = _settings.Mac.StateKind;
        MacEmphasisComboBox.SelectedItem = _settings.Mac.EmphasisKind;
        MacGlassStyleComboBox.SelectedItem = _settings.Mac.GlassStyle;
        MacGlassStyleComboBox.IsEnabled = OperatingSystem.IsMacOSVersionAtLeast(26) && _settings.Material == GlassMaterial.LiquidGlass;
        MacGlassStyleNoteText.IsVisible = OperatingSystem.IsMacOS() && (!OperatingSystem.IsMacOSVersionAtLeast(26) || _settings.Material != GlassMaterial.LiquidGlass);

        _updatingControls = false;
    }

    private static string BuildBackendLabel(WindowGlassSettings settings)
    {
        if (OperatingSystem.IsWindows())
        {
            var backdrop = settings.Windows.BackdropKind == WindowGlassWindowsBackdropKind.Preset
                ? MapPresetBackdrop(settings.Material)
                : settings.Windows.BackdropKind.ToString();

            return $"DWM {backdrop}";
        }

        if (OperatingSystem.IsMacOS())
        {
            if (settings.Material == GlassMaterial.LiquidGlass && OperatingSystem.IsMacOSVersionAtLeast(26))
            {
                var style = settings.Mac.GlassStyle == WindowGlassMacGlassStyleKind.Preset
                    ? WindowGlassMacGlassStyleKind.Regular.ToString()
                    : settings.Mac.GlassStyle.ToString();

                return $"AppKit liquid/{style}";
            }

            var material = settings.Mac.VisualMaterial == WindowGlassMacMaterialKind.Preset
                ? MapPresetMacMaterial(settings.Material)
                : settings.Mac.VisualMaterial.ToString();

            return $"AppKit {material}";
        }

        return "Transparent shell";
    }

    private static string MapPresetBackdrop(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Mica => WindowGlassWindowsBackdropKind.MainWindow.ToString(),
            GlassMaterial.Acrylic or GlassMaterial.Blur => WindowGlassWindowsBackdropKind.TransientWindow.ToString(),
            GlassMaterial.Auto => WindowGlassWindowsBackdropKind.Auto.ToString(),
            _ => WindowGlassWindowsBackdropKind.TabbedWindow.ToString()
        };

    private static string MapPresetMacMaterial(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Acrylic or GlassMaterial.Blur => WindowGlassMacMaterialKind.Menu.ToString(),
            GlassMaterial.Mica => WindowGlassMacMaterialKind.HeaderView.ToString(),
            GlassMaterial.MicaAlt => WindowGlassMacMaterialKind.Sidebar.ToString(),
            GlassMaterial.Auto => WindowGlassMacMaterialKind.AppearanceBased.ToString(),
            _ => WindowGlassMacMaterialKind.Popover.ToString()
        };

    private static string FormatColor(Color color) => $"#{color.ToUInt32():X8}";

    private static bool TryParseColor(string? value, out Color color) => Color.TryParse(value, out color);

    private void UpdateShared(Func<WindowGlassSettings, WindowGlassSettings> update)
    {
        if (_updatingControls)
        {
            return;
        }

        _settings = update(_settings);
        ApplySettings();
    }

    private void UpdateWindows(Func<WindowGlassWindowsSettings, WindowGlassWindowsSettings> update) =>
        UpdateShared(settings => settings with { Windows = update(settings.Windows) });

    private void UpdateMac(Func<WindowGlassMacSettings, WindowGlassMacSettings> update) =>
        UpdateShared(settings => settings with { Mac = update(settings.Mac) });

    private static T GetSelectedEnum<T>(ComboBox comboBox, T fallback)
        where T : struct, Enum =>
        comboBox.SelectedItem is T value ? value : fallback;

    private void OnPresetClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string tag } && Enum.TryParse<GlassMaterial>(tag, out var material))
        {
            ApplyPreset(material);
        }
    }

    private void OnMaterialSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateShared(settings => settings with
        {
            Material = GetSelectedEnum(MaterialComboBox, settings.Material)
        });

    private void OnCornerRadiusChanged(object? sender, RangeBaseValueChangedEventArgs e) =>
        UpdateShared(settings => settings with
        {
            CornerRadius = Math.Clamp(e.NewValue, 0, 56)
        });

    private void OnTintColorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        if (TryParseColor(TintColorTextBox.Text, out var color))
        {
            _settings = _settings with { TintColor = color };
            ApplySettings();
            return;
        }

        UpdateControlValues();
    }

    private void OnWindowsBackdropSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            BackdropKind = GetSelectedEnum(WindowsBackdropComboBox, windows.BackdropKind)
        });

    private void OnWindowsHostBackdropSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            HostBackdropBrush = GetSelectedEnum(WindowsHostBackdropComboBox, windows.HostBackdropBrush)
        });

    private void OnWindowsDarkModeSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            ImmersiveDarkMode = GetSelectedEnum(WindowsDarkModeComboBox, windows.ImmersiveDarkMode)
        });

    private void OnWindowsCornerSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            CornerKind = GetSelectedEnum(WindowsCornerComboBox, windows.CornerKind)
        });

    private void OnWindowsBorderModeSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            BorderColorMode = GetSelectedEnum(WindowsBorderModeComboBox, windows.BorderColorMode)
        });

    private void OnWindowsBorderColorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        if (TryParseColor(WindowsBorderColorTextBox.Text, out var color))
        {
            UpdateWindows(windows => windows with { BorderColor = color });
            return;
        }

        UpdateControlValues();
    }

    private void OnWindowsCaptionModeSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            CaptionColorMode = GetSelectedEnum(WindowsCaptionModeComboBox, windows.CaptionColorMode)
        });

    private void OnWindowsCaptionColorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        if (TryParseColor(WindowsCaptionColorTextBox.Text, out var color))
        {
            UpdateWindows(windows => windows with { CaptionColor = color });
            return;
        }

        UpdateControlValues();
    }

    private void OnWindowsTextModeSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateWindows(windows => windows with
        {
            TextColorMode = GetSelectedEnum(WindowsTextModeComboBox, windows.TextColorMode)
        });

    private void OnWindowsTextColorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        if (TryParseColor(WindowsTextColorTextBox.Text, out var color))
        {
            UpdateWindows(windows => windows with { TextColor = color });
            return;
        }

        UpdateControlValues();
    }

    private void OnMacMaterialSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateMac(mac => mac with
        {
            VisualMaterial = GetSelectedEnum(MacMaterialComboBox, mac.VisualMaterial)
        });

    private void OnMacBlendingSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateMac(mac => mac with
        {
            BlendingKind = GetSelectedEnum(MacBlendingComboBox, mac.BlendingKind)
        });

    private void OnMacStateSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateMac(mac => mac with
        {
            StateKind = GetSelectedEnum(MacStateComboBox, mac.StateKind)
        });

    private void OnMacEmphasisSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateMac(mac => mac with
        {
            EmphasisKind = GetSelectedEnum(MacEmphasisComboBox, mac.EmphasisKind)
        });

    private void OnMacGlassStyleSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateMac(mac => mac with
        {
            GlassStyle = GetSelectedEnum(MacGlassStyleComboBox, mac.GlassStyle)
        });

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnZoomClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private readonly record struct Preset(string Title, string Description, Color TintColor);
}
