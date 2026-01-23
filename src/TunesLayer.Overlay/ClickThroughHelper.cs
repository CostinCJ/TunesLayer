using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TunesLayer.Overlay;

/// <summary>
/// Helper class for managing click-through window behavior.
/// This is essential for anti-cheat safety as it ensures the overlay
/// doesn't interfere with game input.
/// </summary>
public static class ClickThroughHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    /// <summary>
    /// Enables click-through mode for a window.
    /// Mouse events will pass through to the window below.
    /// </summary>
    public static void EnableClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    /// <summary>
    /// Disables click-through mode for a window.
    /// Window will receive mouse events normally.
    /// </summary>
    public static void DisableClickThrough(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
    }

    /// <summary>
    /// Checks if a window has click-through mode enabled.
    /// </summary>
    public static bool IsClickThroughEnabled(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return false;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        return (extendedStyle & WS_EX_TRANSPARENT) != 0;
    }
}
