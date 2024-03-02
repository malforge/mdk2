using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace Mdk.CommandLine.Interactive;

[SupportedOSPlatform("windows")]
public static class Clipboard
{
    const uint CF_UNICODETEXT = 13;
    const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll")]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    static extern bool EmptyClipboard();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalUnlock(IntPtr hMem);

    /// <summary>
    /// Put the specified text on the clipboard.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static Task<bool> PutAsync(string text) =>
        // Try for a maximum of 5 seconds
        Task.Run(() =>
        {
            var start = DateTime.Now;
            while (DateTime.Now - start < TimeSpan.FromSeconds(5))
            {
                if (PutCore(text))
                    return true;
                Task.Delay(100).Wait();
            }
            return false;
        });

    static bool PutCore(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            Console.WriteLine("Failed to open clipboard.");
            return false;
        }

        var global = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((text.Length + 1) * 2));
        if (global == IntPtr.Zero)
        {
            CloseClipboard();
            Console.WriteLine("Failed to allocate memory.");
            return false;
        }

        var lockedMem = GlobalLock(global);
        if (lockedMem == IntPtr.Zero)
        {
            CloseClipboard();
            Console.WriteLine("Failed to lock global memory.");
            return false;
        }

        try
        {
            Marshal.Copy(text.ToCharArray(), 0, lockedMem, text.Length);
            // Set the end of the string to null terminators
            Marshal.Copy(new[] { '\0' }, 0, IntPtr.Add(lockedMem, text.Length * 2), 1);
            EmptyClipboard();
            SetClipboardData(CF_UNICODETEXT, global);
            return true;
        }
        finally
        {
            GlobalUnlock(global);
            CloseClipboard();
        }
    }
}