Process memory library
==============

A library that simplifies the process of reading and writing to
the memory of another process in a .NET langauge.

__Remember to compile with /unsafe__

Example of a program which simply reads an array
of 16 bytes from address 0x1D8153A4 in the 'program' process and then
it prints out those bytes to the console.
When passing the name of a process you can write it with or without
'.exe' at the end of the string.
```C#
unsafe class Program
{
	static void Main(string[] args)
	{
		var pm = new ProcessMemory("program.exe", Access.Read);

		// The address the library will read from
		long address = 0x1D8153A4;

		// Check if the process is running currently
		if (pm.ProcessExists)
		{
			// Read an array of bytes from the address
			byte[] bytes = pm.ReadArray<byte>(address, 16);

			// Print out each byte to the console
			foreach (byte b in bytes)
				Console.WriteLine("0x{0:X2}", b);
		}
		Console.Read();
	}
}
```

Example of a program that reads an int from memory, then
adds 10 to it and writes it back to the same address.
```C#
unsafe class Program
{
	static void Main(string[] args)
	{
		// Implicitly using Access.ReadAndWrite
		var pm = new ProcessMemory("program.exe");

		long address = 0x1D8153A4;

		if (pm.ProcessExists)
		{
			int number;

			number = pm.Read<int>(address);
			number += 10;
			
			pm.Write<int>(address, number);
		}
	}
}
```