using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Avaritis.Memory
{
    /// <summary>
    /// Use this class to read memory from another process running
    /// on the computer.
    /// The address passed to the Read and Write methods has to
    /// be the base address (This is the address of the process
    /// plus the offset from that address to a given variable/struct).
    /// 
    /// Note that this class is "unsafe", if you're going to use it you
    /// will have to compile the program with /unsafe.
    /// </summary>
    public unsafe class ProcessMemory : IDisposable
    {
        #region Variables

        private IntPtr hWnd, hProcess;
        private Access access;

        #endregion

        #region Constructor and Destoructor

        public ProcessMemory(
            string className,
            string windowName,
            Access access = Access.AllAccess)
        {
            this.Disposed = false;
            this.access = access;
            this.hWnd = User32.FindWindow(className, windowName);
            this.hProcess = GetProcessHandleFromWindowHandle(hWnd);
        }

        public ProcessMemory(
            string processName,
            int processIndex = 0,
            Access access = Access.AllAccess)
        {
            this.Disposed = false;

            // Remove ".exe" from the process name passed if it contains ".exe".
            if (Regex.Match(processName, "\\w+\\.exe").Success)
                processName = Regex.Match(processName, "(!?\\w+)\\.exe").Groups[1].Value;

            this.access = access;
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                this.hWnd = processes[processIndex].MainWindowHandle;
                this.hProcess = GetProcessHandleFromWindowHandle(hWnd);
            }
            else
            {
                throw new IndexOutOfRangeException(string.Format("No \"{0}\" process found.", processName));
            }
        }

        #endregion

        #region Getters and Setters

        public bool Disposed
        {
            get; private set;
        }

        /// <summary>
        /// Returns true if the process still runs.
        /// False if it doens't.
        /// 
        /// Use before you call a read or write method to
        /// make sure the process is still running.
        /// </summary>
        public bool ProcessRunning
        {
            get
            {
                this.hProcess = GetProcessHandleFromWindowHandle(hWnd);
                return hProcess != IntPtr.Zero;
            }
        }

        public IntPtr WindowHandle
        {
            get { return this.hWnd; }
        }

        public IntPtr ProcessHandle
        {
            get { return this.hProcess; }
        }

        public Access Access
        {
            get
            {
                return this.access;
            }
            set
            {
                this.access = value;
                this.hProcess = GetProcessHandleFromWindowHandle(hWnd);
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Calls OpenProcess from Kernel32.dll to get the
        /// process handle.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        private IntPtr GetProcessHandleFromWindowHandle(IntPtr hWnd)
        {
            IntPtr hProcess = IntPtr.Zero;

            int pID = 0;
            User32.GetWindowThreadProcessId(hWnd, &pID);

            switch (this.access)
            {
                case Access.AllAccess:
                    hProcess = Kernel32.OpenProcess(
                        Kernel32.PROCESS_ALL_ACCESS, false, pID);
                    break;
                case Access.Read:
                    hProcess = Kernel32.OpenProcess(
                        Kernel32.PROCESS_VM_READ, false, pID);
                    break;
                case Access.Write:
                    hProcess = Kernel32.OpenProcess(
                        Kernel32.PROCESS_VM_WRITE, false, pID);
                    break;
            }

            return hProcess;
        }

        /// <summary>
        /// Converts a generic type to a byte array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        private byte[] GetBytes<T>(T type) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] byteArray = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(type, ptr, true);
            Marshal.Copy(ptr, byteArray, 0, size);
            Marshal.FreeHGlobal(ptr);
            return byteArray;
        }

        #endregion

        #region Read from memory

        /// <summary>
        /// The native read method. This method directly
        /// calls ReadProcessMemory() from Kernel32.dll.
        /// You should use the generic read method as this
        /// one only complicates the code and makes it
        /// less readable.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool Read(long address, void* buffer, int size)
        {
            if (access == Access.Read || access == Access.AllAccess)
                return Kernel32.ReadProcessMemory(hProcess, new IntPtr(address), buffer, size, null);
            else
                throw new InvalidAccessException("Can not read with Access.Write");
        }

        /// <summary>
        /// Generic read method. Any struct can be used.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public T Read<T>(long address) where T : struct
        {
            if (access == Access.Read || access == Access.AllAccess)
            {
                int size = Marshal.SizeOf(typeof(T));
                fixed (byte* bytePtr = new byte[size])
                {
                    Kernel32.ReadProcessMemory(hProcess, new IntPtr(address), bytePtr, size, null);
                    IntPtr ptr = new IntPtr(bytePtr);
                    return (T)Marshal.PtrToStructure(ptr, typeof(T));
                }
            }
            else
            {
                throw new InvalidAccessException("Can not read with Access.Write");
            }
        }

        public T[] ReadArray<T>(long address, int count) where T : struct
        {
            int typeSize = Marshal.SizeOf(typeof(T));
            return ReadArray<T>(address, count, typeSize);
        }

        /// <summary>
        /// Generic read array method. With this method you
        /// can read an array from memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="typeSize"></param>
        /// <returns></returns>
        public T[] ReadArray<T>(long address, int count, int typeSize) where T : struct
        {
            if (access == Access.Read || access == Access.AllAccess)
            {
                int arraySize = count * typeSize;

                fixed (byte* bytePtr = new byte[arraySize])
                {
                    Kernel32.ReadProcessMemory(hProcess, new IntPtr(address), bytePtr, arraySize, null);
                    T[] array = new T[count];
                    IntPtr ptr = new IntPtr(bytePtr);
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = (T)Marshal.PtrToStructure(ptr, typeof(T));
                        ptr = new IntPtr(ptr.ToInt64() + typeSize);
                    }
                    return array;
                }
            }
            else
            {
                throw new InvalidAccessException("Can not read with Access.Write");
            }
        }

        /// <summary>
        /// Reads an ASCII string from memory.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string ReadString(long address, int count)
        {
            return ReadString(address, count, Encoding.ASCII);
        }

        /// <summary>
        /// Reads a string from memory with the specified encoding.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ReadString(long address, int count, Encoding encoding)
        {
            if (access == Access.Read || access == Access.AllAccess)
            {
                return encoding.GetString(ReadArray<byte>(address, count, 1));
            }
            else
            {
                throw new InvalidAccessException("Can not read with Access.Write");
            }
        }

        #endregion

        #region Write to memory

        /// <summary>
        /// Native write method. This method calls the
        /// WriteProcessMemory() function from Kernel32.dll directly.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool Write(long address, void* buffer, int size)
        {
            if (access == Access.Write || access == Access.AllAccess)
                return Kernel32.WriteProcessMemory(hProcess, new IntPtr(address), buffer, size, null);
            else
                throw new InvalidAccessException("Can not write with Access.Read");
        }

        /// <summary>
        /// Write a generic type to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Write<T>(long address, T data) where T : struct
        {
            if (access == Access.Write || access == Access.AllAccess)
            {
                int size = Marshal.SizeOf(typeof(T));
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, ptr, true);
                bool result = Write(address, ptr.ToPointer(), size);
                Marshal.FreeHGlobal(ptr);
                return result;
            }
            else
            {
                throw new InvalidAccessException("Can not write with Access.Read");
            }
        }

        /// <summary>
        /// Write a generic array to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public bool WriteArray<T>(long address, T[] array) where T : struct
        {
            if (access == Access.Write || access == Access.AllAccess)
            {
                if (array == null || array.Length == 0)
                    return false;

                int typeSize = Marshal.SizeOf(typeof(T));
                int arraySize = typeSize * array.Length;
                
                byte[] byteArray = new byte[arraySize];

                int offset = 0;
                foreach (T type in array)
                {
                    byte[] typeBytes = GetBytes<T>(type);
                    Array.Copy(typeBytes, 0, byteArray, offset, typeSize);
                    offset += typeSize;
                }

                fixed (byte* bytePtr = new byte[arraySize])
                {
                    for (int i = 0; i < arraySize; i++)
                        bytePtr[i] = byteArray[i];
                    return Write(address, bytePtr, arraySize);
                }
            }
            else
            {
                throw new InvalidAccessException("Can not write with Access.Read");
            }
        }

        /// <summary>
        /// Writes an ASCII encoded string to memory.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool WriteString(long address, string text)
        {
            return WriteString(address, text, Encoding.ASCII);
        }

        /// <summary>
        /// Write a string to memory with the specified encoding.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public bool WriteString(long address, string text, Encoding encoding)
        {
            if (access == Access.Write || access == Access.AllAccess)
            {
                return WriteArray(address, encoding.GetBytes(text));
            }
            else
            {
                throw new InvalidAccessException("Can not write with Access.Read");
            }
        }

        #endregion

        #region Implemented methods

        /// <summary>
        /// Closes the Process handle.
        /// </summary>
        public void Dispose()
        {
            if (!this.Disposed)
            {
                if (hProcess != IntPtr.Zero)
                    Kernel32.CloseHandle(hProcess);

                this.hWnd = IntPtr.Zero;
                this.hProcess = IntPtr.Zero;

                this.Disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// This enum specifies the access level used
    /// in the ProcessMemory class.
    /// 
    /// AllAccess grants access for both reading and writing to memory.
    /// Read only grants access for reading to memory.
    /// Write only grants access for writing to memory.
    /// </summary>
    public enum Access
    {
        AllAccess,
        Read,
        Write
    }
}
