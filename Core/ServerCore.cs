using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Service;
using System;
using System.Net.Sockets;

namespace MinecraftTunnel.Core
{
    public class ServerCore : IServerCore
    {
        public readonly ILogger ILogger;                               // 日志
        public readonly IConfiguration IConfiguration;                 // 配置文件
        public readonly SemaphoreService SemaphoreService;

        protected byte[] ReceiveBuffer;

        private IServiceScope ServiceScope;
        private Socket Socket;
        private PlayerToken playerToken;

        public readonly SocketAsyncEventArgs ReceiveEventArgs;
        public readonly SocketAsyncEventArgs SendEventArgs;

        #region 事件
        public static ServerReceive OnServerReceive;
        public static ServerSend OnServerSend;
        public static PlayerLeave OnClose;
        #endregion

        public ServerCore(ILogger<ServerCore> ILogger, IConfiguration IConfiguration, SemaphoreService SemaphoreService)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;
            this.SemaphoreService = SemaphoreService;

            ReceiveBuffer = new byte[ushort.MaxValue];

            ReceiveEventArgs = new SocketAsyncEventArgs
            {
                UserToken = this
            };
            ReceiveEventArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
            SendEventArgs = new SocketAsyncEventArgs
            {
                UserToken = this
            };

            ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }

        public void SendPacket(byte[]? buffer, int offset, int count)
        {
            SendEventArgs.SetBuffer(buffer, offset, count);
            Socket.SendAsync(SendEventArgs);
        }
        public void SendPacket(byte[] Packet)
        {
            SendEventArgs.SetBuffer(Packet);
            Socket.SendAsync(SendEventArgs);
        }
        public void Accpet(Socket Socket, PlayerToken playerToken)
        {
            this.Socket = Socket;
            this.playerToken = playerToken;
        }
        public void Start(IServiceScope ServiceScope)
        {
            this.ServiceScope = ServiceScope;
            bool willRaiseEvent = Socket.ReceiveAsync(ReceiveEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(ReceiveEventArgs);
            }
        }
        /// <summary>
        /// 每当套接字上完成接收或发送操作时，都会调用此方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">与完成的接收操作关联的SocketAsyncEventArg</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        /// <summary>
        /// 消息发送回调
        /// </summary>
        /// <param name="socketAsync"></param>
        private void ProcessSend(SocketAsyncEventArgs socketAsync)
        {
            int count = SendEventArgs.BytesTransferred;
            int offset = SendEventArgs.Offset;
            byte[] buffer = SendEventArgs.Buffer;
            byte[] packet = new byte[count];
            Array.Copy(buffer, offset, packet, 0, count);
            OnServerSend?.Invoke(playerToken, packet);
        }
        /// <summary>
        /// 消息处理的回调
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessReceive(SocketAsyncEventArgs socketAsync)
        {
            int offset = ReceiveEventArgs.Offset;
            int count = ReceiveEventArgs.BytesTransferred;
            byte[] Buffer = ReceiveEventArgs.Buffer;
            if (count > 0 && ReceiveEventArgs.SocketError == SocketError.Success)
            {
                byte[] packet = new byte[count];
                Array.Copy(Buffer, offset, packet, 0, count);
                OnServerReceive?.Invoke(playerToken, packet);
                bool willRaiseEvent = Socket.ReceiveAsync(ReceiveEventArgs);
                if (!willRaiseEvent)
                    ProcessReceive(ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(playerToken);
            }
        }
        private void CloseClientSocket(PlayerToken playerToken)
        {
            OnClose.Invoke(playerToken);
        }
        public void Stop()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();

            SemaphoreService.Semaphore.Release();

            ServiceScope.Dispose();
        }
    }
}
