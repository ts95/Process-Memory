using System;
using System.Runtime.InteropServices;

namespace Avaritis.Memory
{
    internal class Kernel32
    {
        public const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;

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
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("Kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr hObject);
    }
}
