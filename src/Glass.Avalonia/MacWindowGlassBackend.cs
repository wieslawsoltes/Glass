using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace Glass.Avalonia;

internal sealed partial class MacWindowGlassBackend : IWindowGlassBackend
{
    private const nuint SizableMask = 2u | 16u;

    private MacHost? _host;

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

        var wantsLiquid = settings.Material == GlassMaterial.LiquidGlass && ObjectiveC.ClassExists("NSGlassEffectView");
        if (wantsLiquid)
        {
            if (_host is not { Kind: MacHostKind.LiquidWrapper })
            {
                _host?.Dispose();
                _host = CreateLiquidHost(nsWindow, nsView);
            }

            if (_host is not null)
            {
                UpdateLiquidHost(_host.View, settings);
            }

            return;
        }

        if (_host is not { Kind: MacHostKind.VisualEffectWrapper })
        {
            _host?.Dispose();
            _host = CreateVisualEffectHost(nsWindow, nsView);
        }

        if (_host is not null)
        {
            UpdateVisualEffectHost(_host.View, settings);
        }
    }

    public void Reset(Window window)
    {
        _host?.Dispose();
        _host = null;
    }

    private static void ConfigureWindow(IntPtr nsWindow)
    {
        ObjectiveC.SendVoid(nsWindow, "setOpaque:", false);
        ObjectiveC.SendVoid(nsWindow, "setMovableByWindowBackground:", true);
        ObjectiveC.SendVoid(nsWindow, "setTitlebarAppearsTransparent:", true);
        ObjectiveC.SendVoidHandle(nsWindow, "setBackgroundColor:", ObjectiveC.GetClearColor());
    }

    private static MacHost CreateVisualEffectHost(IntPtr nsWindow, IntPtr nsView)
    {
        var currentContentView = ObjectiveC.SendIntPtr(nsWindow, "contentView");
        if (currentContentView == nsView)
        {
            var contentWrapper = ObjectiveC.CreateView("NSVisualEffectView", ObjectiveC.SendRect(nsView, "frame"));
            ObjectiveC.SendVoid(contentWrapper, "setAutoresizingMask:", SizableMask);
            ObjectiveC.SendVoid(nsView, "removeFromSuperview");
            ObjectiveC.SendVoidRect(nsView, "setFrame:", ObjectiveC.SendRect(contentWrapper, "bounds"));
            ObjectiveC.SendVoidHandle(contentWrapper, "addSubview:", nsView);
            ObjectiveC.SendVoidHandle(nsWindow, "setContentView:", contentWrapper);

            return new MacHost(MacHostKind.VisualEffectWrapper, contentWrapper, nsView, IntPtr.Zero, nsWindow, true);
        }

        var parentView = ObjectiveC.SendIntPtr(nsView, "superview");
        if (parentView == IntPtr.Zero)
        {
            return new MacHost(MacHostKind.VisualEffectWrapper, IntPtr.Zero, nsView, parentView, nsWindow, false);
        }

        var visualEffectView = ObjectiveC.CreateView("NSVisualEffectView", ObjectiveC.SendRect(nsView, "frame"));
        ObjectiveC.SendVoid(visualEffectView, "setAutoresizingMask:", SizableMask);
        ObjectiveC.SendVoid(nsView, "removeFromSuperview");
        ObjectiveC.SendVoidRect(nsView, "setFrame:", ObjectiveC.SendRect(visualEffectView, "bounds"));
        ObjectiveC.SendVoidHandle(visualEffectView, "addSubview:", nsView);
        ObjectiveC.SendVoidHandle(parentView, "addSubview:", visualEffectView);

        return new MacHost(MacHostKind.VisualEffectWrapper, visualEffectView, nsView, parentView, nsWindow, false);
    }

    private static MacHost CreateLiquidHost(IntPtr nsWindow, IntPtr nsView)
    {
        var currentContentView = ObjectiveC.SendIntPtr(nsWindow, "contentView");
        if (currentContentView == nsView)
        {
            var contentWrapper = ObjectiveC.CreateView("NSGlassEffectView", ObjectiveC.SendRect(nsView, "frame"));
            ObjectiveC.SendVoid(contentWrapper, "setAutoresizingMask:", SizableMask);
            ObjectiveC.SendVoid(nsView, "removeFromSuperview");
            ObjectiveC.SendVoidHandle(contentWrapper, "setContentView:", nsView);
            ObjectiveC.SendVoidHandle(nsWindow, "setContentView:", contentWrapper);

            return new MacHost(MacHostKind.LiquidWrapper, contentWrapper, nsView, IntPtr.Zero, nsWindow, true);
        }

        var parentView = ObjectiveC.SendIntPtr(nsView, "superview");
        if (parentView == IntPtr.Zero)
        {
            return new MacHost(MacHostKind.LiquidWrapper, IntPtr.Zero, nsView, parentView, nsWindow, false);
        }

        var glassView = ObjectiveC.CreateView("NSGlassEffectView", ObjectiveC.SendRect(nsView, "frame"));
        ObjectiveC.SendVoid(glassView, "setAutoresizingMask:", SizableMask);
        ObjectiveC.SendVoid(nsView, "removeFromSuperview");
        ObjectiveC.SendVoidHandle(glassView, "setContentView:", nsView);
        ObjectiveC.SendVoidHandle(parentView, "addSubview:", glassView);

        return new MacHost(MacHostKind.LiquidWrapper, glassView, nsView, parentView, nsWindow, false);
    }

    private static void UpdateVisualEffectHost(IntPtr visualEffectView, WindowGlassSettings settings)
    {
        if (visualEffectView == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoidInteger(visualEffectView, "setMaterial:", MapVisualMaterial(settings.Material));
        ObjectiveC.SendVoidInteger(visualEffectView, "setBlendingMode:", 0);
        ObjectiveC.SendVoidInteger(visualEffectView, "setState:", 0);
        ObjectiveC.SendVoid(visualEffectView, "setEmphasized:", true);
    }

    private static void UpdateLiquidHost(IntPtr glassView, WindowGlassSettings settings)
    {
        if (glassView == IntPtr.Zero)
        {
            return;
        }

        ObjectiveC.SendVoidInteger(glassView, "setStyle:", 0);
        ObjectiveC.SendVoid(glassView, "setCornerRadius:", settings.CornerRadius);
        ObjectiveC.SendVoidHandle(glassView, "setTintColor:", ObjectiveC.CreateColor(settings.TintColor));
    }

    private static nint MapVisualMaterial(GlassMaterial material) =>
        material switch
        {
            GlassMaterial.Acrylic or GlassMaterial.Blur => 13,
            GlassMaterial.MicaAlt => 12,
            GlassMaterial.LiquidGlass => 13,
            _ => 21
        };

    private sealed class MacHost : IDisposable
    {
        private bool _disposed;

        public MacHost(MacHostKind kind, IntPtr view, IntPtr contentView, IntPtr parentView, IntPtr window, bool installedAsWindowContentView)
        {
            Kind = kind;
            View = view;
            ContentView = contentView;
            ParentView = parentView;
            Window = window;
            InstalledAsWindowContentView = installedAsWindowContentView;
        }

        public MacHostKind Kind { get; }

        public IntPtr View { get; }

        public IntPtr ContentView { get; }

        public IntPtr ParentView { get; }

        public IntPtr Window { get; }

        public bool InstalledAsWindowContentView { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (View == IntPtr.Zero)
            {
                return;
            }

            if (InstalledAsWindowContentView && Window != IntPtr.Zero && ContentView != IntPtr.Zero)
            {
                ObjectiveC.SendVoid(ContentView, "removeFromSuperview");
                if (Kind == MacHostKind.LiquidWrapper)
                {
                    ObjectiveC.SendVoidHandle(View, "setContentView:", IntPtr.Zero);
                }

                ObjectiveC.SendVoidHandle(Window, "setContentView:", ContentView);
            }
            else if (ParentView != IntPtr.Zero && ContentView != IntPtr.Zero)
            {
                ObjectiveC.SendVoid(ContentView, "removeFromSuperview");
                if (Kind == MacHostKind.LiquidWrapper)
                {
                    ObjectiveC.SendVoidHandle(View, "setContentView:", IntPtr.Zero);
                }

                ObjectiveC.SendVoidHandle(ParentView, "addSubview:", ContentView);
            }

            ObjectiveC.SendVoid(View, "removeFromSuperview");
            ObjectiveC.Release(View);
        }
    }

    private enum MacHostKind
    {
        VisualEffectWrapper,
        LiquidWrapper
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

        public static IntPtr GetClearColor()
        {
            var colorClass = LookupClass("NSColor");
            return SendIntPtr(colorClass, "clearColor");
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

        public static IntPtr SendIntPtr(IntPtr receiver, string selector, double arg1, double arg2, double arg3, double arg4) =>
            IntPtr_objc_msgSend_Double4(receiver, GetSelector(selector), arg1, arg2, arg3, arg4);

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
        private static partial IntPtr IntPtr_objc_msgSend_Double4(IntPtr receiver, IntPtr selector, double arg1, double arg2, double arg3, double arg4);

        [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
        private static partial NativeRect NativeRect_objc_msgSend(IntPtr receiver, IntPtr selector);

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
