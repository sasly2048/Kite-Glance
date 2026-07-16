using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace KiteGlance.Interop;

/// <summary>
/// Gives a WPF window the OS's own material: acrylic backdrop, rounded corners,
/// and the layered system shadow.
///
/// The usual WPF trick (AllowsTransparency=true) is a trap: it forces the window
/// into a software-composited layered surface, which disables DWM entirely --
/// so you end up hand-painting a fake glass gradient that doesn't actually
/// sample the wallpaper behind it, and you inherit blurry text as a bonus.
///
/// The right path is to leave the HWND opaque to WPF, punch the composition
/// target transparent, and let DWM own the backdrop.
/// </summary>
public static class WindowMaterial
{
    // DWMWINDOWATTRIBUTE
    private const int UseImmersiveDarkMode = 20;
    private const int CornerPreference = 33;
    private const int SystemBackdropType = 38;

    // DWM_WINDOW_CORNER_PREFERENCE
    private const int RoundLarge = 2;

    // DWM_SYSTEMBACKDROP_TYPE
    private const int Mica = 2;
    private const int Acrylic = 3;      // transient window; blurs what's behind
    private const int MicaAlt = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left, Right, Top, Bottom;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(
        IntPtr hwnd, ref Margins margins);

    /// <summary>
    /// Call from Window.SourceInitialized. Returns false if the OS is too old,
    /// in which case the caller should fall back to a solid background.
    /// </summary>
    public static bool Apply(Window window, bool acrylic = true)
    {
        try
        {
            var source = (HwndSource?)PresentationSource.FromVisual(window);
            if (source is null) return false;

            var hwnd = source.Handle;

            // Let the DWM backdrop show through WPF's composition surface.
            source.CompositionTarget.BackgroundColor = Colors.Transparent;

            // Extend the frame into the whole client area, otherwise the
            // backdrop only paints under the (nonexistent) titlebar.
            var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref margins);

            var dark = 1;
            DwmSetWindowAttribute(hwnd, UseImmersiveDarkMode, ref dark, sizeof(int));

            var corner = RoundLarge;
            DwmSetWindowAttribute(hwnd, CornerPreference, ref corner, sizeof(int));

            // acrylic=false now means NO system backdrop at all: the window
            // paints its own opaque surface. We still take DWM's corners,
            // dark mode, and shadow above.
            if (acrylic)
            {
                var backdrop = Acrylic;
                var hr = DwmSetWindowAttribute(hwnd, SystemBackdropType, ref backdrop, sizeof(int));
                return hr == 0;
            }

            return true;
        }
        catch
        {
            return false;   // pre-22H2, or DWM disabled
        }
    }
}
