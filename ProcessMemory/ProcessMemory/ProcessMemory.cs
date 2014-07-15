using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TS95.Memory
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
    public unsafe class ProcessMemory
    {
        #region Variables

        private IntPtr hWnd, hProcess;
        private AccessLevel access;

        #endregion

        #region Constructor and Destructor

        public ProcessMemory(
            string className,
            string windowName,
            AccessLevel access = AccessLevel.All)
        {
            this.access = access;
            this.hWnd = NativeMethods.FindWindowW(className, windowName);
            this.hProcess = GetProcessHandleFromWindowHandle(hWnd);
        }

        public ProcessMemory(
            string processName,
            int processIndex = 0,
            AccessLevel access = AccessLevel.All)
        {
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

        ~ProcessMemory()
        {
            if (hProcess != IntPtr.Zero)
                NativeMethods.CloseHandle(hProcess);
        }

        #endregion

        #region Getters and Setters

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

        public AccessLevel Access
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
            NativeMethods.GetWindowThreadProcessId(hWnd, &pID);

            switch (access)
            {
                case AccessLevel.Read:
                    hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM_READ, false, pID);
                    break;

                case AccessLevel.Write:
                    hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM_WRITE, false, pID);
                    break;

                default:
                    hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, pID);
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

        private void VirtualProtectExWithAccessLevel(long address, uint size, out uint oldProtectType)
        {
            switch (access)
            {
                case AccessLevel.Read:
                    VirtualProtectEx(address, (UIntPtr)size, NativeMethods.PROCESS_VM_READ, out oldProtectType);
                    break;

                case AccessLevel.Write:
                    VirtualProtectEx(address, (UIntPtr)size, NativeMethods.PROCESS_VM_WRITE, out oldProtectType);
                    break;

                default:
                    VirtualProtectEx(address, (UIntPtr)size, NativeMethods.PROCESS_ALL_ACCESS, out oldProtectType);
                    break;
            }
        }

        private void VirtualProtectEx(long address, UIntPtr size, uint newProtectionType, out uint oldProtectionType)
        {
            // This function is throwing an exception for unknown reasons
            NativeMethods.VirtualProtectEx(hProcess, new IntPtr(address), size, newProtectionType, out oldProtectionType);
        }
        #endregion

        #region Read from memory

        /// <summary>
        /// The native read method. This method directly
        /// calls ReadProcessMemory() from Kernel32.dll.
        /// You should use the Read&lt;T&gt; method instead
        /// of this method.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public void Read(long address, void* buffer, int size)
        {
            switch (access)
            {
                case AccessLevel.Read:
                case AccessLevel.All:
                    uint oldProtectType;
                    VirtualProtectExWithAccessLevel(address, (uint)size, out oldProtectType);
                    if (!NativeMethods.ReadProcessMemory(hProcess, new IntPtr(address), buffer, size, null))
                        throw new LastWin32Exception();
                    VirtualProtectEx(address, (UIntPtr)size, oldProtectType, out oldProtectType);
                    break;

                default:
                    throw new InvalidAccessException("Can not write with Access.Read");
            }
        }

        /// <summary>
        /// Generic read method.
        /// Reads a struct from memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        public T Read<T>(long address) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            fixed (byte* bytePtr = new byte[size])
            {
                Read(address, bytePtr, size);
                return (T)Marshal.PtrToStructure(new IntPtr(bytePtr), typeof(T));
            }
        }

        /// <summary>
        /// Generic read array method.
        /// Reads an array of structs from memory.
        /// 
        /// <b>This overload of ReadArray figures out the type
        /// size by using <c>Marshal.Sizeof(typeof(T))</c></b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public T[] ReadArray<T>(long address, int count) where T : struct
        {
            int typeSize = Marshal.SizeOf(typeof(T));
            return ReadArray<T>(address, count, typeSize);
        }

        /// <summary>
        /// Generic read array method.
        /// Reads an array of structs from memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="typeSize"></param>
        /// <returns></returns>
        public T[] ReadArray<T>(long address, int count, int typeSize) where T : struct
        {
            int arraySize = count * typeSize;

            fixed (byte* bytePtr = new byte[arraySize])
            {
                Read(address, bytePtr, arraySize);
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

        /// <summary>
        /// Reads an ASCII string from memory.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count">Number of bytes to read</param>
        /// <returns></returns>
        public string ReadString(long address, int count)
        {
            return ReadString(address, count, Encoding.ASCII);
        }

        /// <summary>
        /// Reads a string from memory with the specified encoding.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ReadString(long address, int count, Encoding encoding)
        {
            return encoding.GetString(ReadArray<byte>(address, count));
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
        public void Write(long address, void* buffer, int size)
        {
            switch (access)
            {
                case AccessLevel.Write:
                case AccessLevel.All:
                    uint oldProtectType;
                    VirtualProtectExWithAccessLevel(address, (uint)size, out oldProtectType);
                    if (!NativeMethods.WriteProcessMemory(hProcess, new IntPtr(address), buffer, size, null))
                        throw new LastWin32Exception();
                    VirtualProtectEx(address, (UIntPtr)size, oldProtectType, out oldProtectType);
                    break;

                default:
                    throw new InvalidAccessException("Can not write with Access.Read");
            }
        }

        /// <summary>
        /// Write a generic type to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void Write<T>(long address, T data) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Write(address, ptr.ToPointer(), size);
            Marshal.FreeHGlobal(ptr);
        }

        /// <summary>
        /// Write a generic array to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        public void WriteArray<T>(long address, T[] array) where T : struct
        {
            if (array == null || array.Length == 0)
                throw new ArgumentException("Invalid array (either empty or null)");

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

                Write(address, bytePtr, arraySize);
            }
        }

        /// <summary>
        /// Writes an ASCII encoded string to memory.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public void WriteString(long address, string text)
        {
            WriteString(address, text, Encoding.ASCII);
        }

        /// <summary>
        /// Write a string to memory with the specified encoding.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public void WriteString(long address, string text, Encoding encoding)
        {
            WriteArray(address, encoding.GetBytes(text));
        }

        #endregion
    }

    /// <summary>
    /// This enum specifies the access level used
    /// in the ProcessMemory class.
    /// 
    /// All grants access for both reading and writing to memory.
    /// Read only grants access for reading to memory.
    /// Write only grants access for writing to memory.
    /// </summary>
    public enum AccessLevel : byte
    {
        /// <summary>Equivalent to: PROCESS_ALL_ACCESS</summary>
        All,
        /// <summary>Equivalent to: PROCESS_VM_READ</summary>
        Read,
        /// <summary>Equivalent to: PROCESS_VM_WRITE</summary>
        Write
    }
}
