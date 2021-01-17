using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Service;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MinecraftTunnel.Core
{
    public class ClientCore
    {
        public readonly ILogger Logger;                               // 日志
        public readonly IConfiguration Configuration;                 // 配置文件
        private Socket Socket;                                         // Socket
        private SocketAsyncEventArgs SendEventArgs;
        private SocketAsyncEventArgs ReceiveEventArgs;


        private byte[] ReceiveBuffer = new byte[ushort.MaxValue];
        private PlayerToken PlayerToken;

        #region 事件
        public static TunnelReceive OnTunnelReceive;
        public static TunnelSend OnTunnelSend;
        public static PlayerLeave OnClose;
        #endregion

        public ClientCore(ILogger Logger, IConfiguration Configuration)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
        }
        public void SendPacket(byte[] Packet)
        {
            SendEventArgs.SetBuffer(Packet);
            Socket.SendAsync(SendEventArgs);
        }
        public void Start(PlayerToken PlayerToken)
        {
            this.PlayerToken = PlayerToken;
            IPAddress ipaddr;
            if (!IPAddress.TryParse(Configuration["Nat:IP"], out ipaddr))
            {
                IPAddress[] iplist = Dns.GetHostAddresses(Configuration["Nat:IP"]);
                if (iplist != null && iplist.Length > 0)
                {
                    ipaddr = iplist[0];
                }
            }

            IPEndPoint localEndPoint = new IPEndPoint(ipaddr, Configuration.GetValue<int>("Nat:Port"));
            Socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.UserToken = PlayerToken;

            SocketAsyncEventArgs connSocketAsyncEventArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = localEndPoint
            };

            connSocketAsyncEventArgs.Completed += IO_Completed;
            Socket.Connect(localEndPoint);
            ProcessConnect(connSocketAsyncEventArgs);

            return;
            connSocketAsyncEventArgs.Completed += IO_Completed;
            if (!Socket.ConnectAsync(connSocketAsyncEventArgs))
            {
                ProcessConnect(connSocketAsyncEventArgs);
            }
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                default:
                    Logger.LogError("套接字上完成的最后一个操作不是接收或发送或连接。");
                    throw new ArgumentException("套接字上完成的最后一个操作不是接收或发送或连接。");
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (ReceiveEventArgs.BytesTransferred > 0 && ReceiveEventArgs.SocketError == SocketError.Success)
            {
                if (OnTunnelReceive != null)
                {
                    byte[] Buffer = new byte[ReceiveEventArgs.BytesTransferred];
                    Array.Copy(ReceiveEventArgs.Buffer, ReceiveEventArgs.Offset, Buffer, 0, ReceiveEventArgs.BytesTransferred);
                    OnTunnelReceive(PlayerToken, Buffer);
                }
                bool willRaiseEvent = Socket.ReceiveAsync(ReceiveEventArgs);
                if (!willRaiseEvent)
                    ProcessReceive(ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (OnTunnelSend != null)
            {
                byte[] Buffer = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, Buffer, 0, e.BytesTransferred);
                OnTunnelSend.Invoke(PlayerToken, Buffer);
            }
        }
        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ReceiveEventArgs = new SocketAsyncEventArgs();
                ReceiveEventArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
                ReceiveEventArgs.Completed += IO_Completed;
                if (!Socket.ReceiveAsync(ReceiveEventArgs))
                {
                    ProcessReceive(ReceiveEventArgs);
                }
                PlayerToken.Login("mc.hypixel.net", 65535);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            // OnClose.Invoke(PlayerToken);
        }
        public void Stop()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
    }
}
