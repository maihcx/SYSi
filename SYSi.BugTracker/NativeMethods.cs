using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SYSi.BugTracker
{
    public class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd, ref Margins margins);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Margins { public int Left, Right, Top, Bottom; }

        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        internal const int DWMWA_CAPTION_COLOR = 35;
        internal const int DWMWA_TEXT_COLOR = 36;
        internal const int DWMWA_BORDER_COLOR = 34;
        internal const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        internal const int DWMSBT_MAINWINDOW = 2; // Mica
    }
}
