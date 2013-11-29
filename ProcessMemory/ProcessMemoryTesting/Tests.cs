using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProcessMemoryTesting
{
    class Tests
    {
        static void Main(string[] args)
        {
            RegexTest();
        }

        /// <summary>
        /// In the ProcessMemory constructor a regex is used
        /// to remove .exe from the string passed to the constructor.
        /// 
        /// This test checks if it works like expected.
        /// </summary>
        static void RegexTest()
        {
            // Dummy name
            string processName = "whatever.exe";

            if (Regex.Match(processName, "\\w+\\.exe").Success)
                processName = Regex.Match(processName, "(!?\\w+)\\.exe").Groups[1].Value;

            Console.WriteLine("Expected result: whatever");
            Console.WriteLine("Actual result: " + processName);

            if (processName == "whatever")
            {
                Console.WriteLine("Test succeded");
            }
            else
            {
                Console.Error.WriteLine("Test failed");
            }

            Console.Read();
        }
    }
}
