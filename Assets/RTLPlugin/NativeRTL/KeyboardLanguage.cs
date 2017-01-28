using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

internal static class KeyboardLanguage
{
    public static CultureInfo Get()
    {
        return GetCurrentKeyboardLayout();
    }

#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
    [DllImport("user32.dll")]
    static extern IntPtr GetKeyboardLayout(uint thread);
#endif

    private static CultureInfo GetCurrentKeyboardLayout()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundProcess = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            int keyboardLayout = GetKeyboardLayout(foregroundProcess).ToInt32() & 0xFFFF;
            return new CultureInfo(keyboardLayout);
        }
        catch (Exception _)
        {
            Debug.LogError(_);
            return new CultureInfo(1033); // Assume English if something went wrong.
        }
#else
        return new CultureInfo(1033);
#endif
    }

}
