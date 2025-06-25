using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

public static class HotkeyService
{
    private const int HOTKEY_ID = 9000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    public static void RegisterHotkey(Window window, string hotkeyString)
    {
        UnregisterHotKey(GetHandle(window), HOTKEY_ID);

        ParseHotkey(hotkeyString, out uint modifiers, out uint key);
        RegisterHotKey(GetHandle(window), HOTKEY_ID, modifiers, key);
    }

    public static void UnregisterHotkey(Window window)
    {
        if (window == null)
            return;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        UnregisterHotKey(handle, HOTKEY_ID);
    }

    public static void AttachHotkeyListener(Window window, Action callback)
    {
        var source = HwndSource.FromHwnd(GetHandle(window));
        source?.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                callback();
                handled = true;
            }
            return IntPtr.Zero;
        });
    }

    private static IntPtr GetHandle(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Cannot register hotkey: window handle is not yet created.");
        return handle;
    }


    private static void ParseHotkey(string hotkey, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;

        if (hotkey.Contains("Ctrl")) modifiers |= MOD_CONTROL;
        if (hotkey.Contains("Alt")) modifiers |= MOD_ALT;
        if (hotkey.Contains("Shift")) modifiers |= MOD_SHIFT;
        if (hotkey.Contains("Win")) modifiers |= MOD_WIN;

        var parts = hotkey.Split('+');
        var last = parts[^1];

        key = (uint)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), last, true));
    }
}
