using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Service;
using System;
using System.Net;
using System.Net.Sockets;

namespace MinecraftTunnel.Core
{
    public class ServerListen
    {
        public readonly ILogger Logger;                               // 日志
        public readonly IConfiguration Configuration;                 // 配置文件
        private readonly IServiceProvider ServiceProvider;            // 服务

        private Socket ServerSocket;                                    // Socket
        private SemaphoreService SemaphoreService;

        public ServerListen(ILogger<ServerListen> Logger, IConfiguration Configuration, IServiceProvider ServiceProvider, SemaphoreService SemaphoreService)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.ServiceProvider = ServiceProvider;
            this.SemaphoreService = SemaphoreService;
        }
        public void Listen()
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress iPAddress = IPAddress.Parse(Configuration["Server:IP"]);
            IPEndPoint serverIP = new IPEndPoint(iPAddress, Configuration.GetValue<int>("Server:Port"));
            ServerSocket.Bind(serverIP);
            ServerSocket.NoDelay = true;
            ServerSocket.Listen(100);
            StartAccept(null);
        }
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
            SemaphoreService.Semaphore.WaitOne();
            // Accpet 成功
            bool willRaiseEvent = ServerSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs socketAsync)
        {
            ProcessAccept(socketAsync);
        }
        /// <summary>
        /// accept 异步回调
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            PlayerToken playerToken = new PlayerToken();
            using (IServiceScope serviceScope = ServiceProvider.GetService<IServiceScopeFactory>().CreateScope())
            {
                ServerCore serverCore = serviceScope.ServiceProvider.GetService<ServerCore>();
                playerToken.ServerCore = serverCore;
                serverCore.Accpet(e.AcceptSocket, playerToken);
                serverCore.Start(serviceScope);
            }
            // 接受后面的连接请求
            StartAccept(e);
        }
    }
}
