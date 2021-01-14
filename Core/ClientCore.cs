using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Service;
using System;
using System.Net;
using System.Net.Sockets;

namespace MinecraftTunnel.Core
{
    public class ClientCore
    {
        public readonly ILogger Logger;                               // 日志
        public readonly IConfiguration Configuration;                 // 配置文件
        public Socket Socket;                                   // Socket
        public SocketAsyncEventArgs SendEventArgs;
        private SocketAsyncEventArgs receiveSocketAsyncEventArgs;
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

            SocketAsyncEventArgs connSocketAsyncEventArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = localEndPoint
            };
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
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                if (OnTunnelReceive != null)
                {
                    byte[] Buffer = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, Buffer, 0, e.BytesTransferred);
                    OnTunnelReceive.Invoke(PlayerToken, Buffer);
                }
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
                receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
                receiveSocketAsyncEventArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
                receiveSocketAsyncEventArgs.Completed += IO_Completed;
                if (!Socket.ReceiveAsync(receiveSocketAsyncEventArgs))
                {
                    ProcessReceive(receiveSocketAsyncEventArgs);
                }
            }
        }

        public void SendAsync(byte[] packet)
        {
            throw new NotImplementedException();
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            OnClose.Invoke(PlayerToken);
        }
        public void Stop()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }
    }
}
