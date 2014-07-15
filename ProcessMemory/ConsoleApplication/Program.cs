using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS95.Memory;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessMemory pm = new ProcessMemory("Skype", 0, AccessLevel.Read);
            byte[] bytes = pm.ReadArray<byte>(0x1D8153A4, 16);

            Console.WriteLine(bytes);
            Console.ReadLine();
        }
    }
}
