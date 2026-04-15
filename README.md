# Glass

`Glass` is a minimal Avalonia workspace for native glass-like window materials on macOS and Windows.

The shared library lives in [src/Glass.Avalonia/Glass.Avalonia.csproj](/Users/wieslawsoltes/GitHub/Glass/src/Glass.Avalonia/Glass.Avalonia.csproj) and exposes attached properties through `WindowGlass`.

The sample app lives in [samples/Glass.Sample/Glass.Sample.csproj](/Users/wieslawsoltes/GitHub/Glass/samples/Glass.Sample/Glass.Sample.csproj) and lets you switch between liquid-glass, mica, mica-alt, and acrylic-style presets at runtime.

## Native mapping

- Windows 11 uses `DwmSetWindowAttribute` with `DWMWA_SYSTEMBACKDROP_TYPE`, immersive dark mode, host backdrop brush, and rounded-corner hints.
- macOS uses `NSVisualEffectView` today and upgrades to `NSGlassEffectView` automatically when the host exposes the macOS 26 AppKit liquid-glass API.
- Other hosts fall back to Avalonia transparency hints so the shell stays usable.

## Build

Run the sample on macOS with:

```bash
dotnet run --project samples/Glass.Sample/Glass.Sample.csproj -f net10.0
```

Run the sample on Windows with:

```bash
dotnet run --project samples/Glass.Sample/Glass.Sample.csproj -f net10.0-windows10.0.19041.0
```

If you want the solution to compile explicit Apple bindings under `net10.0-macos`, install the `macos` workload and enable:

```bash
dotnet workload restore
dotnet build -p:EnableMacOSWorkloadTargeting=true
```
