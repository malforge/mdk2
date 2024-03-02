using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using static Mdk.CommandLine.Interactive.Win32;
// ReSharper disable InconsistentNaming

namespace Mdk.CommandLine.Interactive;

[SupportedOSPlatform("windows")]
public class ToastWindow : IDisposable
{
    const string ClassName = "Mdk2ToastWindow";
    const int AnimationTime = 300;


    const int IDC_HAND = 32649;

    static readonly IntPtr HInstance = Marshal.GetHINSTANCE(typeof(ToastWindow).Module);

    readonly ConcurrentQueue<Action> _actions = new();
    readonly List<object?> _contentList;

    readonly List<Content> _contents = new();
    readonly IntPtr _hCursorArrow = LoadCursor(IntPtr.Zero, IDC_ARROW);
    readonly IntPtr _hCursorHand = LoadCursor(IntPtr.Zero, IDC_HAND);

    readonly IntPtr _hFont;

    readonly IntPtr _hHyperlinkFont;

    IntPtr _hWnd;
    // Create a font that looks like a hyperlink

    ToastWindow(List<object?> contentList)
    {
        _contentList = contentList;

        _hFont = CreateFont(-12, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 0, 0, "Segoe UI");
        _hHyperlinkFont = CreateFont(-12, 0, 0, 0, 400, 0, 1, 0, 1, 0, 0, 0, 0, "Segoe UI");
    }

    public void Dispose()
    {
        if (_hWnd != IntPtr.Zero)
            DestroyWindow(_hWnd);
        DeleteObject(_hFont);
        DeleteObject(_hHyperlinkFont);
    }

    public static async Task ShowAsync(int duration, object? firstContent, params object?[] contents)
    {
        var contentList = new List<object?> { firstContent };
        contentList.AddRange(contents);
        var tcs = new TaskCompletionSource<bool>();
        var toast = new ToastWindow(contentList);
        var thread = new Thread(toast.Run);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(tcs);
        await Task.Delay(duration).ConfigureAwait(false);
        toast.RequestClose();
        await tcs.Task.ConfigureAwait(false);
    }

    public static async Task ShowAsync(object? firstContent, params object?[] contents) => await ShowAsync(30000, firstContent, contents).ConfigureAwait(false);

    public void RequestClose() => RunOnThread(Hide);

    void Run(object? obj)
    {
        var tcs = (TaskCompletionSource<bool>)obj!;
        Show();
        tcs.SetResult(true);
    }

    void Show()
    {
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
            style = 0,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate((WndProc)WindowProc),
            cbClsExtra = 0,
            cbWndExtra = 0,
            hInstance = HInstance,
            hIcon = IntPtr.Zero,
            hCursor = IntPtr.Zero,
            hbrBackground = COLOR_INFOBK + 1,
            lpszMenuName = null,
            lpszClassName = ClassName,
            hIconSm = IntPtr.Zero
        };

        RegisterClassEx(ref wc);

        var workArea = GetWorkArea();

        _hWnd = CreateWindowEx(
            WS_EX_TOPMOST,
            ClassName,
            "MDK2",
            WS_POPUPWINDOW,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            HInstance,
            IntPtr.Zero);

        foreach (var content in _contentList)
        {
            if (content is ToastHyperlink hyperlink)
                _contents.Add(new Hyperlink(this, hyperlink));
            else if (content is not null)
                _contents.Add(new Text(this, content.ToString() ?? string.Empty));
        }
        _contents.Add(new CloseButton(this));

        var desiredWindowWidth = 0;
        var desiredWindowHeight = 0;
        const int WindowMargin = 10;
        const int ItemSpacing = 8;
        var currentX = WindowMargin;
        var currentY = WindowMargin;
        foreach (var content in _contents)
        {
            content.Move(currentX, currentY);
            currentX += content.Width + ItemSpacing;
            desiredWindowWidth = Math.Max(desiredWindowWidth, currentX);
            desiredWindowHeight = Math.Max(desiredWindowHeight, currentY + content.Height);
        }
        desiredWindowWidth += WindowMargin;
        desiredWindowHeight += WindowMargin;

        // Then center all content vertically, without changing the horizontal position
        foreach (var content in _contents)
            content.Move(content.Left, (desiredWindowHeight - content.Height) / 2);

        var x = (workArea.Right - workArea.Left - desiredWindowWidth) / 2;
        var y = workArea.Bottom - desiredWindowHeight - 10;
        SetWindowPos(_hWnd, IntPtr.Zero, x, y, desiredWindowWidth, desiredWindowHeight, 0);

        AnimateWindow(_hWnd, 300, AW_VER_NEGATIVE | AW_SLIDE);

        while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            if (msg.message == WM_RUNONTHREAD)
            {
                while (_actions.TryDequeue(out var action))
                    action();
            }
            else
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        foreach (var content in _contents)
            content.Dispose();
    }

    void Hide()
    {
        AnimateWindow(_hWnd, AnimationTime, AW_HIDE | AW_VER_POSITIVE | AW_SLIDE);
        DestroyWindow(_hWnd);
        _hWnd = IntPtr.Zero;
    }

    IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_MOUSEMOVE:
                SetCursor(_hCursorArrow);
                break;
            case WM_DESTROY:
                Environment.Exit(0);
                break;
            case WM_CTLCOLORSTATIC:
                var content = _contents.Find(c => c.Handle == lParam);
                if (content is not null)
                {
                    var color = GetSysColor(content.TextColor);
                    SetTextColor(wParam, color.ToInt32());
                }
                SetBkMode(wParam, TRANSPARENT);
                return GetStockObject(NULL_BRUSH);
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }


    static RECT GetWorkArea()
    {
        var workArea = new RECT();
        SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);
        return workArea;
    }


    void RunOnThread(Action action)
    {
        _actions.Enqueue(action);
        PostMessage(_hWnd, WM_RUNONTHREAD, IntPtr.Zero, IntPtr.Zero);
    }


    abstract class Content : IDisposable
    {
        protected Content(ToastWindow window)
        {
            Window = window;
        }

        public ToastWindow Window { get; }

        public IntPtr Handle { get; protected set; }
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; protected init; }
        public int Height { get; protected init; }

        public int TextColor { get; protected set; } = COLOR_INFOTEXT;

        public void Dispose() => DestroyWindow(Handle);

        protected SIZE Measure(string text)
        {
            var hdc = GetDC(Handle);
            var hFont = SendMessage(Handle, WM_GETFONT, IntPtr.Zero, IntPtr.Zero);
            SelectObject(hdc, hFont);
            if (!GetTextExtentPoint32(hdc, text, text.Length, out var size))
            {
                size.cx = 0;
                size.cy = 0;
            }
            ReleaseDC(Handle, hdc);
            return size;
        }

        public void Move(int x, int y)
        {
            Left = x;
            Top = y;
            SetWindowPos(Handle, IntPtr.Zero, x, y, Width, Height, 0);
        }
    }

    class Text : Content
    {
        public Text(ToastWindow window, string text) : base(window)
        {
            Handle = CreateWindowEx(
                0,
                "STATIC",
                text,
                WS_CHILD | WS_VISIBLE,
                0,
                0,
                100,
                100,
                window._hWnd,
                IntPtr.Zero,
                HInstance,
                IntPtr.Zero);
            SendMessage(Handle, WM_SETFONT, window._hFont, 1);

            var size = Measure(text);
            Width = size.cx;
            Height = size.cy;

            SetWindowPos(Handle, IntPtr.Zero, 0, 0, size.cx, size.cy, 0);
        }
    }

    class Hyperlink : Content
    {
        readonly ToastHyperlink _hyperlink;
        readonly IntPtr _originalWindowProc;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        readonly WndProc _wndProc;

        bool _isDown = false;

        public Hyperlink(ToastWindow window, ToastHyperlink hyperlink) : base(window)
        {
            _hyperlink = hyperlink;
            Handle = CreateWindowEx(
                0,
                "STATIC",
                hyperlink.Text,
                WS_CHILD | WS_VISIBLE | SS_NOTIFY,
                0,
                0,
                100,
                100,
                window._hWnd,
                IntPtr.Zero,
                HInstance,
                IntPtr.Zero);
            SendMessage(Handle, WM_SETFONT, window._hHyperlinkFont, 1);

            var size = Measure(hyperlink.Text);
            Width = size.cx;
            Height = size.cy;
            TextColor = COLOR_HOTLIGHT;

            SetWindowPos(Handle, IntPtr.Zero, 0, 0, size.cx, size.cy, 0);

            _wndProc = ButtonProc;
            _originalWindowProc = SetWindowLongPtr64(Handle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
        }

        IntPtr ButtonProc(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam)
        {
            switch (msg)
            {
                case WM_MOUSEMOVE:
                    SetCursor(Window._hCursorHand);
                    return CallWindowProc(_originalWindowProc, Handle, msg, wparam, lparam);

                case WM_SETFOCUS:
                    return IntPtr.Zero;

                case WM_LBUTTONUP:
                    GetClientRect(hwnd, out var rect);
                    var pt = new POINT { x = (short)LOWORD(lparam), y = (short)HIWORD(lparam) };
                    if (PtInRect(ref rect, pt))
                    {
                        if (_hyperlink.Callback(_hyperlink))
                            Window.RequestClose();
                    }

                    return CallWindowProc(_originalWindowProc, Handle, msg, wparam, lparam);
            }
            return CallWindowProc(_originalWindowProc, Handle, msg, wparam, lparam);
        }
    }

    class CloseButton : Content
    {
        readonly IntPtr _originalWindowProc;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        readonly WndProc _wndProc;

        public CloseButton(ToastWindow window) : base(window)
        {
            Handle = CreateWindowEx(
                0,
                "BUTTON",
                "X",
                WS_CHILD | WS_VISIBLE | BS_FLAT,
                0,
                0,
                100,
                100,
                window._hWnd,
                IntPtr.Zero,
                HInstance,
                IntPtr.Zero);
            SendMessage(Handle, WM_SETFONT, window._hFont, 1);

            Width = 20;
            Height = 20;
            SetWindowPos(Handle, IntPtr.Zero, 0, 0, Width, Height, 0);

            _wndProc = ButtonProc;
            _originalWindowProc = SetWindowLongPtr64(Handle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
        }

        IntPtr ButtonProc(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam)
        {
            switch (msg)
            {
                case WM_MOUSEMOVE:
                    SetCursor(Window._hCursorHand);
                    break;
                case WM_SETFOCUS:
                    return IntPtr.Zero;
                case WM_LBUTTONUP:
                    GetClientRect(hwnd, out var rect);
                    var pt = new POINT { x = (short)LOWORD(lparam), y = (short)HIWORD(lparam) };
                    if (PtInRect(ref rect, pt))
                        Window.RequestClose();

                    return CallWindowProc(_originalWindowProc, Handle, msg, wparam, lparam);
            }
            return CallWindowProc(_originalWindowProc, Handle, msg, wparam, lparam);
        }
    }
}