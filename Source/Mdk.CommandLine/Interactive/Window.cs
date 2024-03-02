// using System;
// using System.Runtime.InteropServices;
// using static Mdk.CommandLine.Interactive.Win32;
//
// namespace Mdk.CommandLine.Interactive;
//
// public abstract class Window
// {
//     const string ClassName = "Mdk2ToastWindow";
//     static readonly IntPtr HInstance = Marshal.GetHINSTANCE(typeof(ToastWindow).Module);
//
//     static Window()
//     {
//         var wc = new Win32.WNDCLASSEX
//         {
//             cbSize = (uint)Marshal.SizeOf(typeof(Win32.WNDCLASSEX)),
//             style = 0,
//             lpfnWndProc = Marshal.GetFunctionPointerForDelegate((Win32.WndProc)WindowProc),
//             cbClsExtra = 0,
//             cbWndExtra = 0,
//             hInstance = HInstance,
//             hIcon = IntPtr.Zero,
//             hCursor = IntPtr.Zero,
//             hbrBackground = COLOR_INFOBK + 1,
//             lpszMenuName = null,
//             lpszClassName = ClassName,
//             hIconSm = IntPtr.Zero
//         };
//
//         RegisterClassEx(ref wc);
//     }
// }