using System;
using System.Runtime.InteropServices;

namespace TS95.Memory
{
    [Serializable]
    internal class InvalidAccessException : Exception
    {
        public InvalidAccessException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    internal class LastWin32Exception : Exception
    {
        public LastWin32Exception()
            : base(string.Format("Win32 Error Code: {0:X8}", Marshal.GetLastWin32Error()))
        {
        }
    }
}
