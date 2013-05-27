Process memory library
==============

A library that simplifies the process of reading and writing to
the memory of another process a .NET langauge.

Below is an example of a program which simply reads 16 bytes from
a address 0x1D8153A4 in the spotify process.
When passing the name of a process you can write it with or
without .exe at the end of the string.
```C#
unsafe class Program
{
	static void Main(string[] args)
	{
		using (var pm = new ProcessMemory("spotify.exe", 0, Access.Read))
		{
			int address = 0x1D8153A4;

			if (pm.ProcessExists)
			{
				byte[] bytes = pm.ReadArray<byte>(address, 16);

				foreach (byte b in bytes)
					Console.WriteLine("0x{0:X2}", b);
			}
		}
		Console.Read();
	}
}
```

The ProcessMemory class implements IDisposeable, so you'll have to
call Dispose() once you're done with the class unless you use the
"using" keyword like in the example above.