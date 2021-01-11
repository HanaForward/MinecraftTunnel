using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Service;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MinecraftTunnel.Core
{
    public class ServerCore : TunnelCore
    {
        public readonly ILogger ILogger;                               // 日志
        public readonly IConfiguration IConfiguration;                 // 配置文件

        private Semaphore semaphore;                                    // 信号量 控制最大连接数
        private ushort MaxConnections;                                  // 最大连接数
        private Socket ServerSocket;                                    // Socket
        private TokenPool TokenPool;                                    // 连接池

        #region 事件
        public static ServerReceive OnServerReceive;
        public static ServerSend OnServerSend;
        public static OnClose OnClose;
        #endregion

        public ServerCore(ILogger<ServerCore> ILogger, IConfiguration IConfiguration)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;

            _ = ushort.TryParse(IConfiguration["MaxConnections"], out MaxConnections);
            TokenPool = new TokenPool(MaxConnections);
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            PlayerToken playerToken;
            for (int i = 0; i < MaxConnections; i++) //按照连接数建立读写对象
            {
                playerToken = new PlayerToken();
                playerToken.ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                playerToken.SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                TokenPool.Push(playerToken);
            }
        }
        public override void Start(string IP, int Port)
        {
            IPAddress iPAddress = IPAddress.Parse(IP);
            IPEndPoint serverIP = new IPEndPoint(iPAddress, Port);
            ServerSocket.Bind(serverIP);
            ServerSocket.NoDelay = true;

            semaphore = new Semaphore(MaxConnections, MaxConnections);

            ServerSocket.Listen(100);
            StartAccept(null);
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
        /// accept 异步回调
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                // 被主动调用,并设置回调方法
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // 因为被复用所以得主动清除Accpet套接字
                acceptEventArg.AcceptSocket = null;
            }
            // 信号量
            semaphore.WaitOne();
            // Accpet 成功
            bool willRaiseEvent = ServerSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }
        /// <summary>
        /// AcceptAsync回调方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs socketAsync)
        {
            ProcessAccept(socketAsync);
        }
        /// <summary>
        /// 当Accept完成时回调的方法
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            // 从接受端重用池获取一个新的SocketAsyncEventArgs对象
            PlayerToken playerToken = TokenPool.Pop();
            playerToken.ServerSocket = e.AcceptSocket;

            // 异步回调接收数据
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(playerToken.ReceiveEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(playerToken.ReceiveEventArgs);
            }
            // 接受后面的连接请求
            StartAccept(e);
        }
        /// <summary>
        /// 消息发送回调
        /// </summary>
        /// <param name="socketAsync"></param>
        private void ProcessSend(SocketAsyncEventArgs socketAsync)
        {
            PlayerToken playerToken = (PlayerToken)socketAsync.UserToken;

            int count = playerToken.SendEventArgs.BytesTransferred;
            int offset = playerToken.SendEventArgs.Offset;
            byte[] buffer = playerToken.SendEventArgs.Buffer;
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
            PlayerToken playerToken = (PlayerToken)socketAsync.UserToken;
            int offset = playerToken.ReceiveEventArgs.Offset;
            int count = playerToken.ReceiveEventArgs.BytesTransferred;
            byte[] Buffer = playerToken.ReceiveEventArgs.Buffer;
            // 解析数据包
            if (count > 0 && playerToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                byte[] packet = new byte[count];
                Array.Copy(Buffer, offset, packet, 0, count);
                OnServerReceive?.Invoke(playerToken, packet);
                bool willRaiseEvent = playerToken.ServerSocket.ReceiveAsync(playerToken.ReceiveEventArgs);
                if (!willRaiseEvent)
                    ProcessReceive(playerToken.ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(playerToken);
            }
        }
        private void CloseClientSocket(PlayerToken playerToken)
        {
            OnClose.Invoke(playerToken);
            TokenPool.Push(playerToken);
            semaphore.Release();
        }
        public override void Stop()
        {
            ServerSocket.Shutdown(SocketShutdown.Both);
            ServerSocket.Close();
        }
        public override void Dispose()
        {
            ServerSocket.Dispose();
        }
    }
}
