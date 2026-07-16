using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace KiteGlance.Interop;

/// <summary>
/// Glues the widget to the desktop itself, the way Rainmeter does.
///
/// Topmost was the wrong tool for a desktop widget: it floats over every app
/// you open, and a normal window belongs to exactly one virtual desktop -- so
/// a four-finger swipe to another desktop makes it vanish.
///
/// The fix is to reparent the window into the wallpaper layer. Explorer hosts
/// the wallpaper in a WorkerW window (spawned lazily by sending Progman the
/// undocumented-but-decade-stable 0x052C message). A child of that layer:
///
///   - sits UNDER every application window, on the desktop where it belongs
///   - exists on ALL virtual desktops, because the wallpaper does
///   - survives Win+D and trackpad gestures
///   - never appears in Alt+Tab or Task View
///
/// DWM's rounded-corner attribute stops applying to reparented windows, so we
/// clip our own corners with SetWindowRgn while glued, and the region is
/// refreshed whenever the window animates its height.
/// </summary>
public static class DesktopPin
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(
        IntPtr parent, IntPtr after, string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr region, bool redraw);

    /// <summary>Reparent into the wallpaper layer. Returns false if Explorer's
    /// windows can't be found (e.g. shell replaced), in which case the caller
    /// should fall back to a normal window.</summary>
    public static bool Glue(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return false;

        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return false;

        // Ask Progman to spawn the wallpaper WorkerW (no-op if it exists).
        SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0x0, 1000, out _);

        // The WorkerW we want is the sibling AFTER the one hosting the
        // desktop icons (SHELLDLL_DefView).
        var target = IntPtr.Zero;
        var worker = IntPtr.Zero;
        while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
        {
            if (FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                target = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
        }

        // Win11 24H2 sometimes hosts the wallpaper directly under Progman.
        if (target == IntPtr.Zero) target = progman;

        var prev = window.Topmost;
        window.Topmost = false;

        if (SetParent(hwnd, target) == IntPtr.Zero)
        {
            window.Topmost = prev;
            return false;
        }

        ApplyCornerRegion(window);
        return true;
    }

    /// <summary>Back to a normal top-level window.</summary>
    public static void Unglue(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        SetParent(hwnd, IntPtr.Zero);
        SetWindowRgn(hwnd, IntPtr.Zero, true);   // hand corners back to DWM
        WindowMaterial.Apply(window, acrylic: false);
    }

    /// <summary>
    /// Clip our own rounded corners. Call again whenever the window resizes
    /// while glued -- the expand/collapse spring changes Height every frame.
    /// </summary>
    public static void ApplyCornerRegion(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
        var w = (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX);
        var h = (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY);
        if (w <= 0 || h <= 0) return;

        var r = (int)Math.Round(14 * dpi.DpiScaleX);
        var region = CreateRoundRectRgn(0, 0, w + 1, h + 1, r, r);
        SetWindowRgn(hwnd, region, true);   // the OS owns the region after this
    }
}
