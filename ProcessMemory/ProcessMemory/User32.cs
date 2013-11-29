using System;
using System.Runtime.InteropServices;

namespace Avaritis.Memory
{
    public class User32
    {
        [DllImport("User32.dll")]
        public static extern IntPtr FindWindow(
            string lpClassName,
            string lpWindowName);

        [DllImport("User32.dll")]
        public static unsafe extern int GetWindowThreadProcessId(
            IntPtr hWnd,
            int* lpdwProcessId);
    }
}
