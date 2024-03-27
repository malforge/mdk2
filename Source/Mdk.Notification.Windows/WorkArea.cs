using System.Runtime.InteropServices;
using System.Windows;
// ReSharper disable InconsistentNaming

namespace Mdk.Notification.Windows;

public static class WorkArea
{
    const int MDT_EFFECTIVE_DPI = 0;

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("Shcore.dll")]
    static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    public static IntPtr GetMonitorHandleFromMousePosition()
    {
        GetCursorPos(out var cursorPos);

        return MonitorFromPoint(cursorPos, 2 /*MONITOR_DEFAULTTONEAREST*/);
    }

    public static Rect GetMonitorWorkArea(IntPtr monitorHandle)
    {
        var monitorInfo = new MONITORINFO
        {
            cbSize = Marshal.SizeOf(typeof(MONITORINFO))
        };
        GetMonitorInfo(monitorHandle, ref monitorInfo);

        var bounds = new Rect(monitorInfo.rcWork.Left, monitorInfo.rcWork.Top, monitorInfo.rcWork.Right - monitorInfo.rcWork.Left, monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top);

        GetDpiForMonitor(monitorHandle, MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY);

        bounds.Scale(1.0 / (dpiX / 96.0), 1.0 / (dpiY / 96.0));

        return bounds;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct POINT(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}