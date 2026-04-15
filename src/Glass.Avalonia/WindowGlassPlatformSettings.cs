using Avalonia.Media;

namespace Glass.Avalonia;

public enum WindowGlassWindowsBackdropKind
{
    Preset,
    Auto,
    None,
    MainWindow,
    TransientWindow,
    TabbedWindow
}

public enum WindowGlassWindowsCornerKind
{
    Preset,
    Default,
    DoNotRound,
    Round,
    RoundSmall
}

public enum WindowGlassWindowsSwitch
{
    Preset,
    Off,
    On
}

public enum WindowGlassWindowsBorderColorMode
{
    Default,
    Custom,
    None
}

public enum WindowGlassWindowsColorMode
{
    Default,
    Custom
}

public sealed record class WindowGlassWindowsSettings
{
    public WindowGlassWindowsBackdropKind BackdropKind { get; init; } = WindowGlassWindowsBackdropKind.Preset;

    public WindowGlassWindowsCornerKind CornerKind { get; init; } = WindowGlassWindowsCornerKind.Preset;

    public WindowGlassWindowsSwitch HostBackdropBrush { get; init; } = WindowGlassWindowsSwitch.Preset;

    public WindowGlassWindowsSwitch ImmersiveDarkMode { get; init; } = WindowGlassWindowsSwitch.Preset;

    public WindowGlassWindowsBorderColorMode BorderColorMode { get; init; } = WindowGlassWindowsBorderColorMode.Default;

    public Color BorderColor { get; init; } = Colors.White;

    public WindowGlassWindowsColorMode CaptionColorMode { get; init; } = WindowGlassWindowsColorMode.Default;

    public Color CaptionColor { get; init; } = Colors.White;

    public WindowGlassWindowsColorMode TextColorMode { get; init; } = WindowGlassWindowsColorMode.Default;

    public Color TextColor { get; init; } = Colors.White;
}

public enum WindowGlassMacMaterialKind
{
    Preset,
    AppearanceBased,
    Light,
    Dark,
    Titlebar,
    Selection,
    Menu,
    Popover,
    Sidebar,
    MediumLight,
    UltraDark,
    HeaderView,
    Sheet,
    WindowBackground,
    HudWindow,
    FullScreenUi,
    ToolTip,
    ContentBackground,
    UnderWindowBackground,
    UnderPageBackground
}

public enum WindowGlassMacBlendingKind
{
    Preset,
    BehindWindow,
    WithinWindow
}

public enum WindowGlassMacStateKind
{
    Preset,
    FollowsWindowActiveState,
    Active,
    Inactive
}

public enum WindowGlassMacEmphasisKind
{
    Preset,
    Off,
    On
}

public enum WindowGlassMacGlassStyleKind
{
    Preset,
    Regular,
    Clear
}

public sealed record class WindowGlassMacSettings
{
    public WindowGlassMacMaterialKind VisualMaterial { get; init; } = WindowGlassMacMaterialKind.Preset;

    public WindowGlassMacBlendingKind BlendingKind { get; init; } = WindowGlassMacBlendingKind.Preset;

    public WindowGlassMacStateKind StateKind { get; init; } = WindowGlassMacStateKind.Preset;

    public WindowGlassMacEmphasisKind EmphasisKind { get; init; } = WindowGlassMacEmphasisKind.Preset;

    public WindowGlassMacGlassStyleKind GlassStyle { get; init; } = WindowGlassMacGlassStyleKind.Preset;
}
