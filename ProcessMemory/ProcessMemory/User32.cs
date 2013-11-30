using System;
using System.Runtime.InteropServices;

namespace Avaritis.Memory
{
    internal class User32
    {
        [DllImport("User32.dll")]
        internal static extern IntPtr FindWindowW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName);

        [DllImport("User32.dll")]
        internal static unsafe extern int GetWindowThreadProcessId(
            IntPtr hWnd,
            int* lpdwProcessId);
    }
}
