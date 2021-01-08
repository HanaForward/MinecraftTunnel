using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using System;
using System.Net;
using System.Net.Sockets;

namespace MinecraftTunnel.Core
{
    public class ServerCore : TunnelCore
    {
        private readonly ILogger ILogger;
        private readonly IConfiguration IConfiguration;

        private ushort MaxConnections;                                  // 最大连接数
        private Socket ServerSocket;                                    // Socket
        private TokenPool TokenPool;                                    // 连接池

        public ServerCore(ILogger ILogger, IConfiguration IConfiguration)
        {
            _ = ushort.TryParse(IConfiguration["MaxConnections"], out MaxConnections);
            TokenPool = new TokenPool(MaxConnections + 1);
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        }

        public void Bind(string ip, ushort port)
        {
            IPAddress iPAddress = IPAddress.Parse(ip);
            IPEndPoint serverIP = new IPEndPoint(iPAddress, port);
            ServerSocket.Bind(serverIP);
            ServerSocket.NoDelay = true;
        }
        public override void Start()
        {
            ServerSocket.Listen(100);
            StartAccept(null);
        }
        private void StartAccept(object p)
        {
            throw new NotImplementedException();
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
