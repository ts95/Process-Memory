using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Avaritis.Memory;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ProcessMemoryTests
{
    [TestClass]
    public class ProcessMemoryTests
    {
        /// <summary>
        /// In the ProcessMemory constructor a regex is used
        /// to remove .exe from the string passed to the constructor.
        /// 
        /// This test checks if it works like expected.
        /// </summary>
        [TestMethod]
        public void RegexTest()
        {
            // Dummy name
            string processName = "test.exe";

            if (Regex.Match(processName, "\\w+\\.exe").Success)
                processName = Regex.Match(processName, "(!?\\w+)\\.exe").Groups[1].Value;

            Assert.AreEqual(expected: "test", actual: processName);
        }
    }
}
