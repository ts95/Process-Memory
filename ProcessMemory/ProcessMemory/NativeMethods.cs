using System;
using System.Runtime.InteropServices;

namespace TS95.Memory
{
    internal class NativeMethods
    {
        #region Kernel32

        public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const uint PROCESS_VM_READ = 0x0010;
        public const uint PROCESS_VM_WRITE = 0x0020;

        [DllImport("Kernel32.dll")]
        internal static unsafe extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int dwSize,
            int* lpNumberOfBytesRead);

        [DllImport("Kernel32.dll")]
        internal static unsafe extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int dwSize,
            int* lpNumberOfBytesWritten);

        [DllImport("Kernel32.dll")]
        internal static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("Kernel32.dll")]
        internal static extern bool CloseHandle(
            IntPtr hObject);

        [DllImport("Kernel32.dll")]
        internal static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAdress,
            UIntPtr dwSize,
            uint newProtectionType,
            out uint oldProtectionType);

        #endregion

        #region User32

        [DllImport("User32.dll")]
        internal static extern IntPtr FindWindowW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName);

        [DllImport("User32.dll")]
        internal static unsafe extern int GetWindowThreadProcessId(
            IntPtr hWnd,
            int* lpdwProcessId);

        #endregion
    }
}
