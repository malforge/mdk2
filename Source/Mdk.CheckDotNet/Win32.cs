using System.Runtime.InteropServices;

namespace CheckDotNet;

internal static partial class Win32
{
    // [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    // public static partial int MessageBox(IntPtr hWnd, string text, string caption, uint type);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}