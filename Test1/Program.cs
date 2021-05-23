using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Configuration;

namespace Test1
{
    class Program
    {
        public static void Main(string[] args)
        {
            Process proc = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\AdminCK\source\repos\CmdShellServer\CmdShellServer\bin\Debug\CmdShellServer.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                },
                EnableRaisingEvents = false
            };
            proc.Start();
            Console.WriteLine("pid:" + proc.Id);
            Console.ReadLine();
        }
    }
}
