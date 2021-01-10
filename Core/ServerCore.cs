using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Service;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MinecraftTunnel.Core
{
    public class ServerCore : TunnelCore
    {
        private readonly ILogger ILogger;                               // 日志
        private readonly IConfiguration IConfiguration;
        private readonly TotalService totalService;                     // 统计服务
        private readonly AnalysisService analysisService;               // 数据包处理服务

        private Semaphore semaphore;                                    // 信号量 控制最大连接数

        private Dictionary<string, PlayerToken> ValuePairs;             // 玩家存储器

        private ushort MaxConnections;                                  // 最大连接数
        private Socket ServerSocket;                                    // Socket
        private TokenPool TokenPool;                                    // 连接池

        public ClientCore ClientCore;

        public Action<PlayerToken, byte[]> OnReceive { get; internal set; }
        public Action<PlayerToken, byte[]> OnSend { get; internal set; }

        public void Tunnel(PlayerToken playerToken, string iP, ushort port)
        {
            ClientCore = new ClientCore(playerToken);
            ClientCore.Start(iP, port);
        }


        public ServerCore(ILogger<ServerCore> ILogger, IConfiguration IConfiguration, TotalService totalService, AnalysisService analysisService)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;
            this.totalService = totalService;
            this.analysisService = analysisService;

            _ = ushort.TryParse(IConfiguration["MaxConnections"], out MaxConnections);
            TokenPool = new TokenPool(MaxConnections + 1);
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            PlayerToken playerToken;
            for (int i = 0; i < MaxConnections; i++) //按照连接数建立读写对象
            {
                playerToken = new PlayerToken();
                TokenPool.Push(playerToken);
            }
        }

        public override void Start(string IP, int Port)
        {
            IPAddress iPAddress = IPAddress.Parse(IP);
            IPEndPoint serverIP = new IPEndPoint(iPAddress, Port);
            ServerSocket.Bind(serverIP);
            ServerSocket.NoDelay = true;

            semaphore = new Semaphore(MaxConnections, MaxConnections + 1);
            ServerSocket.Listen(100);
            StartAccept(null);
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
            /*
            // 原子操作,增加一个客户端数量
            Interlocked.Increment(ref totalService.TotalPlayer);
            */

            // 从接受端重用池获取一个新的SocketAsyncEventArgs对象
            PlayerToken playerToken = TokenPool.Pop();
            playerToken.SetCompleted(ProcessReceive, null);
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
        /// 消息处理的回调
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessReceive(SocketAsyncEventArgs socketAsync)
        {
            PlayerToken playerToken = (PlayerToken)socketAsync.UserToken;

            int offset = playerToken.ReceiveEventArgs.Offset;
            int count = playerToken.ReceiveEventArgs.BytesTransferred;
            int endOffset = offset + count;
            byte[] Buffer = playerToken.ReceiveEventArgs.Buffer;

            // 解析数据包
            if (count > 0 && playerToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                Block block = new Block(Buffer, offset);
                NormalProtocol baseProtocol = new NormalProtocol();
                do
                {
                    baseProtocol.Analyze(block);
                    analysisService.Analysis(playerToken, baseProtocol.PacketId, baseProtocol.PacketData);
                } while (baseProtocol.block.step < endOffset);

                bool willRaiseEvent = playerToken.ServerSocket.ReceiveAsync(playerToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                    ProcessReceive(playerToken.ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(socketAsync);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            PlayerToken token = e.UserToken as PlayerToken;
            try
            {
                token.ServerSocket.Shutdown(SocketShutdown.Send);
                token.Close();
            }
            catch (Exception ex)
            {
                ILogger.LogError(ex.Message);
            }
            TokenPool.Push(token);
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
