using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace MusicAmp;

public static class KeyboardMediaHookService
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0X0100;

    private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const int VK_MEDIA_NEXT_TRACK = 0xB0;
    private const int VK_MEDIA_PREV_TRACK = 0xB1;
    private const int VK_MEDIA_STOP = 0xB2;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookId = IntPtr.Zero;

    public static event EventHandler<RoutedEventArgs>? PlayButtonPress;
    public static event EventHandler<RoutedEventArgs>? NextButtonPress;
    public static event EventHandler<RoutedEventArgs>? PrevButtonPress;
    public static event EventHandler<RoutedEventArgs>? StopButtonPress;

    public static void StartHook()
    {
        using (var curProcess = Process.GetCurrentProcess())
        {
            if (curProcess is null)
                return;
            using (var curModule = curProcess.MainModule)
            {
                if (curModule is null)
                    return;
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    public static void StopHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wp, IntPtr lp)
    {
        if (nCode >= 0 && wp == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lp);

            switch (vkCode)
            {
                case VK_MEDIA_PLAY_PAUSE:
                    PlayButtonPress?.Invoke(null, new RoutedEventArgs());
                    break;

                case VK_MEDIA_NEXT_TRACK:
                    NextButtonPress?.Invoke(null, new RoutedEventArgs());
                    break;

                case VK_MEDIA_PREV_TRACK:
                    PrevButtonPress?.Invoke(null, new RoutedEventArgs());
                    break;

                case VK_MEDIA_STOP:
                    StopButtonPress?.Invoke(null, new RoutedEventArgs());
                    break;

                default:
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wp, lp);
    }
}