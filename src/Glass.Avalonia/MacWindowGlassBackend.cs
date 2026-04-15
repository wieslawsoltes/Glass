using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace Glass.Avalonia;

internal sealed partial class MacWindowGlassBackend : IWindowGlassBackend
{
    private const nuint SizableMask = 2u | 16u;
    private const nint OrderBelow = -1;

    private MacHost? _liquidHost;
    private IntPtr _tintOverlayView;
    private IntPtr _tintOverlaySuperview;

    public void Apply(Window window, WindowGlassSettings settings)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        if (window.PlatformImpl is not IMacOSTopLevelPlatformHandle platformHandle)
        {
            return;
        }

        var nsWindow = platformHandle.NSWindow;
        var nsView = platformHandle.NSView;
        if (nsWindow == IntPtr.Zero || nsView == IntPtr.Zero)
        {
            return;
        }

        ConfigureWindow(nsWindow);
        ConfigureHostedView(nsView);

        var wantsLiquid = settings.Material == GlassMaterial.LiquidGlass && ObjectiveC.ClassExists("NSGlassEffectView");
        if (wantsLiquid)
        {
            RemoveTintOverlay();

            if (_liquidHost is null)
            {
                _liquidHost = CreateLiquidHost(nsWindow, nsView);
            }

            if (_liquidHost is not null)
            {
                UpdateLiquidHost(_liquidHost.View, settings);
            }

            return;
        }

        _liquidHost?.Dispose();
        _liquidHost = null;

        UpdateBackdropEffect(FindBackdropEffectView(nsWindow, nsView), settings);
        EnsureTintOverlay(nsView, settings.TintColor);
    }

    public void Reset(Window window)
    {
        _liquidHost?.Dispose();
        _liquidHost = null;
        RemoveTintOverlay();
    }

    private static void ConfigureWindow(IntPtr nsWindow)
    {
        ObjectiveC.SendVoid(nsWindow, "setOpaque:", false);
        ObjectiveC.SendVoid(nsWindow, "setMovableByWindowBackground:", true);
        ObjectiveC.SendVoid(nsWindow, "setTitlebarAppearsTransparent:", true);
        ObjectiveC.SendVoidHandle(nsWindow, "setBackgroundColor:", ObjectiveC.GetClearColor());
    }

    private static void ConfigureHostedView(IntPtr nsView)
    {
        ObjectiveC.SendVoid(nsView, "setWantsLayer:", true);

        var layer = ObjectiveC.SendIntPtr(nsView, "layer");
        if (layer == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoidHandle(layer, "setBackgroundColor:", ObjectiveC.GetClearCgColor());
        ObjectiveC.SendVoid(layer, "setOpaque:", false);
    }

    private static IntPtr FindBackdropEffectView(IntPtr nsWindow, IntPtr hostedView)
    {
        var contentView = ObjectiveC.SendIntPtr(nsWindow, "contentView");
        return FindBackdropEffectSubview(contentView, hostedView);
    }

    private static IntPtr FindBackdropEffectSubview(IntPtr rootView, IntPtr hostedView)
    {
        if (rootView == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var subviews = ObjectiveC.SendIntPtr(rootView, "subviews");
        if (subviews == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var count = ObjectiveC.SendNUInt(subviews, "count");
        for (nuint index = 0; index < count; index++)
        {
            var child = ObjectiveC.SendIntPtr(subviews, "objectAtIndex:", index);
            if (child == IntPtr.Zero || child == hostedView)
            {
                continue;
            }

            if (ObjectiveC.IsKindOfClass(child, "NSVisualEffectView") && ObjectiveC.SendNInt(child, "blendingMode") == 0)
            {
                return child;
            }

            var nested = FindBackdropEffectSubview(child, hostedView);
            if (nested != IntPtr.Zero)
            {
                return nested;
            }
        }

        return IntPtr.Zero;
    }

    private void EnsureTintOverlay(IntPtr hostedView, Color tintColor)
    {
        var parentView = ObjectiveC.SendIntPtr(hostedView, "superview");
        if (parentView == IntPtr.Zero)
        {
            RemoveTintOverlay();
            return;
        }

        if (_tintOverlayView == IntPtr.Zero || _tintOverlaySuperview != parentView)
        {
            RemoveTintOverlay();

            _tintOverlayView = ObjectiveC.CreateView("NSView", ObjectiveC.SendRect(hostedView, "frame"));
            _tintOverlaySuperview = parentView;

            ObjectiveC.SendVoid(_tintOverlayView, "setAutoresizingMask:", SizableMask);
            ConfigureTintView(_tintOverlayView, Colors.Transparent);
            ObjectiveC.SendVoidRect(_tintOverlayView, "setFrame:", ObjectiveC.SendRect(parentView, "bounds"));
            ObjectiveC.SendVoid(parentView, "addSubview:positioned:relativeTo:", _tintOverlayView, OrderBelow, hostedView);
        }

        ConfigureTintView(_tintOverlayView, tintColor);
        ObjectiveC.SendVoidRect(_tintOverlayView, "setFrame:", ObjectiveC.SendRect(parentView, "bounds"));
    }

    private void RemoveTintOverlay()
    {
        if (_tintOverlayView != IntPtr.Zero)
        {
            ObjectiveC.SendVoid(_tintOverlayView, "removeFromSuperview");
            ObjectiveC.Release(_tintOverlayView);
            _tintOverlayView = IntPtr.Zero;
            _tintOverlaySuperview = IntPtr.Zero;
        }
    }

    private static void UpdateBackdropEffect(IntPtr visualEffectView, WindowGlassSettings settings)
    {
        if (visualEffectView == IntPtr.Zero)
        {
            return;
        }

        ConfigureTransparentView(visualEffectView, settings.CornerRadius);
        ObjectiveC.SendVoidInteger(visualEffectView, "setMaterial:", ResolveVisualMaterial(settings));
        ObjectiveC.SendVoidInteger(visualEffectView, "setBlendingMode:", ResolveBlendingMode(settings));
        ObjectiveC.SendVoidInteger(visualEffectView, "setState:", ResolveState(settings));
        ObjectiveC.SendVoid(visualEffectView, "setEmphasized:", ResolveEmphasis(settings));
    }

    private static MacHost? CreateLiquidHost(IntPtr nsWindow, IntPtr nsView)
    {
        var currentContentView = ObjectiveC.SendIntPtr(nsWindow, "contentView");
        if (currentContentView == IntPtr.Zero)
        {
            return null;
        }

        var container = ObjectiveC.CreateView("NSView", ObjectiveC.SendRect(currentContentView, "frame"));
        var glassView = ObjectiveC.CreateView("NSGlassEffectView", ObjectiveC.SendRect(currentContentView, "bounds"));

        ObjectiveC.SendVoid(container, "setAutoresizingMask:", SizableMask);
        ObjectiveC.SendVoid(glassView, "setAutoresizingMask:", SizableMask);
        ConfigureTransparentView(container, 0);
        ConfigureTransparentView(glassView, 0);

        ObjectiveC.SendVoid(nsView, "removeFromSuperview");
        ObjectiveC.SendVoidRect(glassView, "setFrame:", ObjectiveC.SendRect(container, "bounds"));
        ObjectiveC.SendVoidRect(nsView, "setFrame:", ObjectiveC.SendRect(container, "bounds"));
        ObjectiveC.SendVoidHandle(container, "addSubview:", glassView);
        ObjectiveC.SendVoidHandle(container, "addSubview:", nsView);
        ObjectiveC.SendVoidHandle(nsWindow, "setContentView:", container);

        return new MacHost(container, nsView, currentContentView, nsWindow, glassView);
    }

    private static void UpdateLiquidHost(IntPtr glassView, WindowGlassSettings settings)
    {
        if (glassView == IntPtr.Zero)
        {
            return;
        }

        ConfigureTransparentView(glassView, settings.CornerRadius);
        ObjectiveC.SendVoidInteger(glassView, "setStyle:", ResolveGlassStyle(settings));
        ObjectiveC.SendVoid(glassView, "setCornerRadius:", settings.CornerRadius);
        ObjectiveC.SendVoidHandle(glassView, "setTintColor:", ObjectiveC.CreateColor(settings.TintColor));
    }

    private static nint ResolveVisualMaterial(WindowGlassSettings settings) =>
        settings.Mac.VisualMaterial switch
        {
            WindowGlassMacMaterialKind.AppearanceBased => 0,
            WindowGlassMacMaterialKind.Light => 1,
            WindowGlassMacMaterialKind.Dark => 2,
            WindowGlassMacMaterialKind.Titlebar => 3,
            WindowGlassMacMaterialKind.Selection => 4,
            WindowGlassMacMaterialKind.Menu => 5,
            WindowGlassMacMaterialKind.Popover => 6,
            WindowGlassMacMaterialKind.Sidebar => 7,
            WindowGlassMacMaterialKind.MediumLight => 8,
            WindowGlassMacMaterialKind.UltraDark => 9,
            WindowGlassMacMaterialKind.HeaderView => 10,
            WindowGlassMacMaterialKind.Sheet => 11,
            WindowGlassMacMaterialKind.WindowBackground => 12,
            WindowGlassMacMaterialKind.HudWindow => 13,
            WindowGlassMacMaterialKind.FullScreenUi => 15,
            WindowGlassMacMaterialKind.ToolTip => 17,
            WindowGlassMacMaterialKind.ContentBackground => 18,
            WindowGlassMacMaterialKind.UnderWindowBackground => 21,
            WindowGlassMacMaterialKind.UnderPageBackground => 22,
            _ => MapVisualMaterial(settings.Material)
        };

    private static nint MapVisualMaterial(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Acrylic or GlassMaterial.Blur => 5,
            GlassMaterial.Mica => 10,
            GlassMaterial.MicaAlt => 7,
            GlassMaterial.LiquidGlass => 21,
            _ => 21
        };

    private static nint ResolveBlendingMode(WindowGlassSettings settings) =>
        settings.Mac.BlendingKind switch
        {
            WindowGlassMacBlendingKind.WithinWindow => 1,
            _ => 0
        };

    private static nint ResolveState(WindowGlassSettings settings) =>
        settings.Mac.StateKind switch
        {
            WindowGlassMacStateKind.FollowsWindowActiveState => 0,
            WindowGlassMacStateKind.Inactive => 2,
            _ => 1
        };

    private static bool ResolveEmphasis(WindowGlassSettings settings) =>
        settings.Mac.EmphasisKind == WindowGlassMacEmphasisKind.On;

    private static nint ResolveGlassStyle(WindowGlassSettings settings) =>
        settings.Mac.GlassStyle == WindowGlassMacGlassStyleKind.Clear ? 1 : 0;

    private static void ConfigureTransparentView(IntPtr view, double cornerRadius)
    {
        if (view == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoid(view, "setWantsLayer:", true);
        var layer = ObjectiveC.SendIntPtr(view, "layer");
        if (layer == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoidHandle(layer, "setBackgroundColor:", ObjectiveC.GetClearCgColor());
        ObjectiveC.SendVoid(layer, "setOpaque:", false);
        ObjectiveC.SendVoid(layer, "setCornerRadius:", cornerRadius);
        ObjectiveC.SendVoid(layer, "setMasksToBounds:", false);
    }

    private static void ConfigureTintView(IntPtr view, Color color)
    {
        if (view == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoid(view, "setWantsLayer:", true);
        var layer = ObjectiveC.SendIntPtr(view, "layer");
        if (layer == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoidHandle(layer, "setBackgroundColor:", ObjectiveC.CreateCgColor(color));
        ObjectiveC.SendVoid(layer, "setOpaque:", false);
    }

    private sealed class MacHost : IDisposable
    {
        private bool _disposed;

        public MacHost(IntPtr containerView, IntPtr hostedView, IntPtr originalContentView, IntPtr window, IntPtr effectView)
        {
            ContainerView = containerView;
            HostedView = hostedView;
            OriginalContentView = originalContentView;
            Window = window;
            View = effectView;
        }

        public IntPtr ContainerView { get; }

        public IntPtr HostedView { get; }

        public IntPtr OriginalContentView { get; }

        public IntPtr Window { get; }

        public IntPtr View { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (ContainerView == IntPtr.Zero || Window == IntPtr.Zero)
            {
                return;
            }

            if (HostedView != IntPtr.Zero)
            {
                ObjectiveC.SendVoid(HostedView, "removeFromSuperview");
            }

            if (OriginalContentView != IntPtr.Zero)
            {
                ObjectiveC.SendVoidHandle(Window, "setContentView:", OriginalContentView);
                if (HostedView != IntPtr.Zero)
                {
                    ObjectiveC.SendVoidRect(HostedView, "setFrame:", ObjectiveC.SendRect(OriginalContentView, "bounds"));
                    ObjectiveC.SendVoidHandle(OriginalContentView, "addSubview:", HostedView);
                }
            }

            ObjectiveC.SendVoid(ContainerView, "removeFromSuperview");
            ObjectiveC.Release(ContainerView);
        }
    }

    private static partial class ObjectiveC
    {
        private const string LibObjC = "/usr/lib/libobjc.A.dylib";

        private static readonly Dictionary<string, IntPtr> Selectors = new(StringComparer.Ordinal);

        public static bool ClassExists(string className) => LookupClass(className) != IntPtr.Zero;

        public static IntPtr CreateView(string className, NativeRect frame)
        {
            var nativeClass = LookupClass(className);
            var allocated = SendIntPtr(nativeClass, "alloc");
            return SendIntPtr(allocated, "initWithFrame:", frame);
        }

        public static IntPtr CreateColor(Color color)
        {
            var colorClass = LookupClass("NSColor");
            return SendIntPtr(
                colorClass,
                "colorWithSRGBRed:green:blue:alpha:",
                color.R / 255d,
                color.G / 255d,
                color.B / 255d,
                color.A / 255d);
        }

        public static IntPtr CreateCgColor(Color color) => GetCgColor(CreateColor(color));

        public static IntPtr GetClearColor()
        {
            var colorClass = LookupClass("NSColor");
            return SendIntPtr(colorClass, "clearColor");
        }

        public static IntPtr GetClearCgColor() => GetCgColor(GetClearColor());

        public static IntPtr GetCgColor(IntPtr nsColor) => SendIntPtr(nsColor, "CGColor");

        public static bool IsKindOfClass(IntPtr receiver, string className)
        {
            var nativeClass = LookupClass(className);
            return nativeClass != IntPtr.Zero && SendBool(receiver, "isKindOfClass:", nativeClass);
        }

        public static IntPtr LookupClass(string className) => objc_lookUpClass(className);

        public static void Release(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                objc_release(handle);
            }
        }

        public static NativeRect SendRect(IntPtr receiver, string selector) => NativeRect_objc_msgSend(receiver, GetSelector(selector));

        public static IntPtr SendIntPtr(IntPtr receiver, string selector) => IntPtr_objc_msgSend(receiver, GetSelector(selector));

        public static IntPtr SendIntPtr(IntPtr receiver, string selector, IntPtr arg1) => IntPtr_objc_msgSend_IntPtr(receiver, GetSelector(selector), arg1);

        public static IntPtr SendIntPtr(IntPtr receiver, string selector, NativeRect arg1) => IntPtr_objc_msgSend_Rect(receiver, GetSelector(selector), arg1);

        public static IntPtr SendIntPtr(IntPtr receiver, string selector, nuint arg1) => IntPtr_objc_msgSend_NUInt(receiver, GetSelector(selector), arg1);

        public static IntPtr SendIntPtr(IntPtr receiver, string selector, double arg1, double arg2, double arg3, double arg4) =>
            IntPtr_objc_msgSend_Double4(receiver, GetSelector(selector), arg1, arg2, arg3, arg4);

        public static bool SendBool(IntPtr receiver, string selector, IntPtr arg1) => Bool_objc_msgSend_IntPtr(receiver, GetSelector(selector), arg1);

        public static nint SendNInt(IntPtr receiver, string selector) => NInt_objc_msgSend(receiver, GetSelector(selector));

        public static nuint SendNUInt(IntPtr receiver, string selector) => NUInt_objc_msgSend(receiver, GetSelector(selector));

        public static void SendVoid(IntPtr receiver, string selector) => Void_objc_msgSend(receiver, GetSelector(selector));

        public static void SendVoidHandle(IntPtr receiver, string selector, IntPtr arg1) => Void_objc_msgSend_IntPtr(receiver, GetSelector(selector), arg1);

        public static void SendVoid(IntPtr receiver, string selector, bool arg1) => Void_objc_msgSend_Bool(receiver, GetSelector(selector), arg1);

        public static void SendVoid(IntPtr receiver, string selector, double arg1) => Void_objc_msgSend_Double(receiver, GetSelector(selector), arg1);

        public static void SendVoidRect(IntPtr receiver, string selector, NativeRect arg1) => Void_objc_msgSend_Rect(receiver, GetSelector(selector), arg1);

        public static void SendVoidInteger(IntPtr receiver, string selector, nint arg1) => Void_objc_msgSend_NInt(receiver, GetSelector(selector), arg1);

        public static void SendVoid(IntPtr receiver, string selector, nuint arg1) => Void_objc_msgSend_NUInt(receiver, GetSelector(selector), arg1);

        public static void SendVoid(IntPtr receiver, string selector, IntPtr arg1, nint arg2, IntPtr arg3) =>
            Void_objc_msgSend_IntPtr_NInt_IntPtr(receiver, GetSelector(selector), arg1, arg2, arg3);

        private static IntPtr GetSelector(string selector)
        {
            lock (Selectors)
            {
                if (!Selectors.TryGetValue(selector, out var handle))
                {
                    handle = sel_registerName(selector);
                    Selectors[selector] = handle;
                }

                return handle;
            }
        }

        [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr objc_lookUpClass(string name);

        [LibraryImport(LibObjC, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr sel_registerName(string name);

        [LibraryImport(LibObjC)]
        private static partial void objc_release(IntPtr value);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend_Rect(IntPtr receiver, IntPtr selector, NativeRect arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend_NUInt(IntPtr receiver, IntPtr selector, nuint arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial IntPtr IntPtr_objc_msgSend_Double4(IntPtr receiver, IntPtr selector, double arg1, double arg2, double arg3, double arg4);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial NativeRect NativeRect_objc_msgSend(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial nint NInt_objc_msgSend(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial nuint NUInt_objc_msgSend(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static partial bool Bool_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend(IntPtr receiver, IntPtr selector);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_Bool(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_Double(IntPtr receiver, IntPtr selector, double arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_Rect(IntPtr receiver, IntPtr selector, NativeRect arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_NInt(IntPtr receiver, IntPtr selector, nint arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_NUInt(IntPtr receiver, IntPtr selector, nuint arg1);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial void Void_objc_msgSend_IntPtr_NInt_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, nint arg2, IntPtr arg3);
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeRect
    {
        public NativeRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; }

        public double Y { get; }

        public double Width { get; }

        public double Height { get; }
    }
}
