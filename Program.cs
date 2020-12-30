using Microsoft.Extensions.Configuration;
using MinecraftTunnel.Model;
using System;
using System.IO;
using System.Net;

namespace MinecraftTunnel
{
    public class Program
    {
        private static ushort MaxConnections;

        public static ConnectConfig ServerConfig;
        public static ConnectConfig NatConfig;

        public static QueryConfig QueryConfig;



        public static IConfigurationRoot Configuration { get; set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddYamlFile("appsettings.yaml");
            Configuration = builder.Build();

            _ = ushort.TryParse(Configuration["MaxConnections"], out MaxConnections);

            ServerConfig = Configuration.GetSection("Server").Get<ConnectConfig>();
            NatConfig = Configuration.GetSection("Nat").Get<ConnectConfig>();
            QueryConfig = Configuration.GetSection("Query").Get<QueryConfig>();

            StateContext stateContext = new StateContext(MaxConnections, ushort.MaxValue);
            stateContext.Init();

#if DEBUG
            //stateContext.OnAccept += Server_OnAccept;
            stateContext.OnReceive += Server_OnReceive;
            stateContext.OnSend += Server_OnSend;
            //stateContext.OnClose += Server_OnClose;
#endif

            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, ServerConfig.Port);
            stateContext.Start(serverIP);

             Console.ReadKey();
        }

        private static void Server_OnAccept()
        {
            Console.WriteLine($"Push已连接");
        }

        private static void Server_OnClose()
        {
            Console.WriteLine($"Push断开");
        }

        private static void Server_OnSend(int arg1, int arg2)
        {
            Console.WriteLine($"Push已发送:{arg1} 长度:{arg2}");
        }

        private static void Server_OnReceive(AsyncUserToken asyncUserToken, byte[] buffer, int arg, int agr1)
        {
            Console.WriteLine(BitConverter.ToString(buffer));
            Console.WriteLine($"Push已接收:{arg} 长度:{agr1}");
        }
    }
}
