using log4net;
using Microsoft.Extensions.Configuration;
using MinecraftTunnel.Model;
using System;
using System.IO;
using System.Net;
using System.Threading;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace MinecraftTunnel
{
    public class Program
    {
        private static ushort MaxConnections;

        public static ConnectConfig ServerConfig;
        public static ConnectConfig NatConfig;
        public static QueryConfig QueryConfig;

        public static string ConnectionString;


        public static readonly ILog log = LogManager.GetLogger("MinecraftTunnel");

        public static IConfigurationRoot Configuration { get; set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddYamlFile("appsettings.yaml");
            Configuration = builder.Build();

            _ = ushort.TryParse(Configuration["MaxConnections"], out MaxConnections);
            ServerConfig = Configuration.GetSection("Server").Get<ConnectConfig>();
            NatConfig = Configuration.GetSection("Nat").Get<ConnectConfig>();
            QueryConfig = Configuration.GetSection("Query").Get<QueryConfig>();

            ConnectionString = Configuration["DataBase:ConnectionString"];

            StateContext stateContext = new StateContext(MaxConnections, ushort.MaxValue);
            stateContext.Init();
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, ServerConfig.Port);
            stateContext.Start(serverIP);

            int CursorTop, OnPlayer = 0;


            while (true)
            {
                Console.Clear();
                Console.WriteLine("##########实时统计##########");
                Console.SetCursorPosition(0, 3);//将光标至于当前行的开始位置
                Console.WriteLine("##########在线玩家##########");
                Console.SetCursorPosition(0, 1);//将光标至于当前行的开始位置

                uint TotalBytesSend = Interlocked.Exchange(ref stateContext.TotalBytesSend, 0);
                uint TotalBytesRead = Interlocked.Exchange(ref stateContext.TotalBytesRead, 0);

                int Top = Console.CursorTop;//记录当前光标位置

                ClearCurrentConsoleLine();
                Console.WriteLine("当前上行速率:" + TotalBytesSend);
                ClearCurrentConsoleLine();
                Console.WriteLine("当前下行速率:" + TotalBytesRead);
                Console.SetCursorPosition(0, 4);

                int temp = stateContext.Online.Count;
                if (OnPlayer > temp)
                {
                    for (int i = 0; i < OnPlayer - temp; i++)
                    {
                        ClearCurrentConsoleLine(4 + temp + i);
                    }
                }

                OnPlayer = temp;
                // 开始打印玩家列表
                try
                {
                    foreach (var token in stateContext.Online)
                    {
                        CursorTop = Console.CursorTop;
                        Console.Write(new string(' ', Console.WindowWidth));//用空格将当前行填满，相当于清除当前行
                        Console.SetCursorPosition(0, Console.CursorTop);//将光标至于当前行的开始位置
                        Console.WriteLine($"玩家ID : {token.Key} 登录时间:{token.Value.ConnectDateTime} 到期时间:{token.Value.EndTime}");
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }

                Console.SetCursorPosition(0, 5);//将光标恢复至开始时的位置
                Thread.Sleep(1000);
            }
        }

        public static void ClearCurrentConsoleLine(int Cursor = 0)
        {
            int currentLineCursor = Console.CursorTop;
            if (Cursor > 0)
            {
                Console.SetCursorPosition(0, Cursor);
            }
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
