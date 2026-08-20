using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia;

namespace ClaudeUsageChecker.App.Tray;

/// <summary>
/// The icon in the notification area, registered directly with Windows.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia brings a <c>TrayIcon</c> of its own, and it was in use here for a
/// long time. It offers exactly two things: a <c>Clicked</c> event for the left
/// button, and a <c>NativeMenu</c> - which under Windows is a real Win32 menu.
/// That menu cannot be styled from inside the process: system font, hairline
/// separators, no border of its own. Beside the windows of this application it
/// looked like something from another decade.
/// </para>
/// <para>
/// There is no right-click event to hang a window of our own on either, so the
/// choice was between an unstyled menu and registering the icon ourselves. This
/// class does the latter: a message-only window receives the mouse messages of
/// the icon and turns them into two events. What is drawn on a right click is
/// then an ordinary Avalonia window, and it looks like the rest.
/// </para>
/// <para>
/// The messages arrive on the thread that created the window - the UI thread -
/// and are dispatched by the message loop Avalonia already runs. No loop of our
/// own, and no second thread.
/// </para>
/// </remarks>
[SupportedOSPlatform("windows")]
internal sealed class WindowsTrayIcon : IDisposable
{
    /// <summary>Our own message for everything the icon reports.</summary>
    private const uint CallbackMessage = WM_APP + 1;

    private readonly WndProc _procedure;
    private readonly uint _taskbarCreated;
    private readonly IntPtr _window;
    private readonly ushort _class;

    private IntPtr _icon;
    private string _toolTip = string.Empty;
    private bool _added;
    private bool _disposed;

    public WindowsTrayIcon()
    {
        // The delegate has to stay alive for as long as the window: Windows
        // keeps only the raw pointer, and a collected delegate is a crash on the
        // next message.
        _procedure = HandleMessage;

        var moduleHandle = GetModuleHandle(null);
        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_procedure),
            hInstance = moduleHandle,
            lpszClassName = "ClaudeUsageCheckerTray"
        };

        _class = RegisterClassEx(ref wc);
        if (_class == 0)
        {
            throw new InvalidOperationException(
                $"The window class for the tray icon could not be registered ({Marshal.GetLastWin32Error()}).");
        }

        _window = CreateWindowEx(
            0, new IntPtr(_class), null, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, moduleHandle, IntPtr.Zero);

        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"The window for the tray icon could not be created ({Marshal.GetLastWin32Error()}).");
        }

        // Explorer sends this when it restarts. Without answering it the icon is
        // gone for good, and the application keeps running with nothing to click.
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    }

    /// <summary>The left button was clicked.</summary>
    public event EventHandler? Clicked;

    /// <summary>The menu was asked for, at that point on the screen.</summary>
    public event EventHandler<PixelPoint>? MenuRequested;

    /// <summary>Sets the icon, replacing whatever was shown before.</summary>
    public void SetIcon(IntPtr icon)
    {
        var previous = _icon;
        _icon = icon;

        Send(_added ? NIM_MODIFY : NIM_ADD);
        _added = true;

        // Only after Windows has taken the new one: destroying it earlier leaves
        // the area drawing an icon that no longer exists.
        if (previous != IntPtr.Zero)
        {
            DestroyIcon(previous);
        }
    }

    /// <summary>
    /// Sets the tooltip. Windows truncates at 127 characters, which is where the
    /// limit in the formatter comes from.
    /// </summary>
    public void SetToolTip(string text)
    {
        _toolTip = text ?? string.Empty;

        if (_added)
        {
            Send(NIM_MODIFY);
        }
    }

    private void Send(uint message)
    {
        var data = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _window,
            uID = 1,
            uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
            uCallbackMessage = CallbackMessage,
            hIcon = _icon,
            szTip = _toolTip
        };

        Shell_NotifyIcon(message, ref data);
    }

    private IntPtr HandleMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == _taskbarCreated && _added)
        {
            _added = false;
            Send(NIM_ADD);
            _added = true;
            return IntPtr.Zero;
        }

        if (message == CallbackMessage)
        {
            switch ((uint)lParam.ToInt64())
            {
                case WM_LBUTTONUP:
                    Clicked?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;

                // Both, because Windows sends the second one for the keyboard
                // route through the context menu key as well.
                case WM_RBUTTONUP:
                case WM_CONTEXTMENU:
                    MenuRequested?.Invoke(this, CursorPosition());
                    return IntPtr.Zero;
            }
        }

        return DefWindowProc(window, message, wParam, lParam);
    }

    private static PixelPoint CursorPosition() =>
        GetCursorPos(out var point) ? new PixelPoint(point.X, point.Y) : new PixelPoint(0, 0);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_added)
        {
            Send(NIM_DELETE);
            _added = false;
        }

        if (_icon != IntPtr.Zero)
        {
            DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }

        if (_window != IntPtr.Zero)
        {
            DestroyWindow(_window);
        }

        if (_class != 0)
        {
            UnregisterClass(new IntPtr(_class), GetModuleHandle(null));
        }
    }

    private const uint WM_APP = 0x8000;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_CONTEXTMENU = 0x007B;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private delegate IntPtr WndProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint exStyle, IntPtr className, string? windowName, uint style,
        int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(IntPtr className, IntPtr instance);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
}
