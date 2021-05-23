using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CmdShellNode
{
    public class SocketHelper
    {
        Socket client;

        public delegate void ReadHandler(string info);

        public event ReadHandler OnReadFinish;

        byte[] receiveBuffer;

        object sync = new object();

        public bool socketSwitch { get; set; } = false;

        public bool IsClosed { get; set; } = true;

        IPEndPoint iPEndPoint;

        public SocketHelper(string ip, int port)
        {
            // 打开开关
            socketSwitch = true;
            IPAddress ipAddress = IPAddress.Parse(ip);
            iPEndPoint = new IPEndPoint(ipAddress, port);
            Start();
        }

        public void Start()
        {
            if (socketSwitch == false)
            {
                Stop();
                return;
            }
            if (IsClosed == false)
            {
                Stop();
            }
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 设置 TCP 心跳，空闲 30 秒检查一次，失败后每 6 秒检查一次
            byte[] inOptionValues = new byte[4 * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)30000).CopyTo(inOptionValues, 4);
            BitConverter.GetBytes((uint)6000).CopyTo(inOptionValues, 8);
            client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            try
            {
                client.Connect(iPEndPoint); //配置服务器IP与端口
                LogHelper.Init.Log("服务器连接成功！");
                receiveBuffer = new byte[1024];
                IsClosed = false;
                StartReceive();
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("服务器连接失败！" + e.Message);
                Stop();
            }
        }

        public void Stop()
        {
            lock (sync)
            {
                try
                {
                    client?.Shutdown(SocketShutdown.Both);
                    client?.Close();
                    client?.Dispose();//素质三连
                }
                catch (Exception e)
                {
                    LogHelper.Init.Log("Stop error: " + e.Message);
                }
                finally
                {
                    IsClosed = true;
                }
            }
        }

        public void StartReceive()
        {
            try
            {
                client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, new object());
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("StartReceive error:" + e.Message);
            }
        }

        StringBuilder msgCache = new StringBuilder();

        public void ReceiveCallback(IAsyncResult re)
        {
            try
            {
                int len = client.EndReceive(re);
                if (len > 0)
                {
                    string info = Encoding.UTF8.GetString(receiveBuffer, 0, len);
                    info = msgCache.Append(info).ToString();
                    int start = 0;
                    int end = info.IndexOf("\n", start);
                    while (end > 0)
                    {
                        string msg = info.Substring(start, end);
                        if (msg.Length > 0)
                        {
                            OnReadFinish(msg);
                        }
                        start = end + 1;
                        end = info.IndexOf("\n", start);
                    }
                    if (start > 0) {
                        msgCache.Clear();
                        if (start < info.Length) {
                            msgCache.Append(info.Substring(start));
                        }
                    }
                }
                if (socketSwitch)
                {
                    client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, new object());
                }
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("ReceiveCallback error:" + e.Message);
                while (socketSwitch)
                {
                    Start();
                    if (IsClosed) { Thread.Sleep(10000); } else { break; };
                }
            }
        }

        public void Write(string str)
        {
            try
            {
                LogHelper.Init.Log("写入：" + str);
                int length = str.Length;
                if (length > 1)
                {
                    lock (sync)
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes(str);
                        client.Send(buffer, buffer.Length, SocketFlags.None);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("Write error:" + e.Message);
            }
        }
    }
}
