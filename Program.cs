using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Core;
using MinecraftTunnel.Model;
using MinecraftTunnel.Service;
using System;
using System.IO;
using System.Net;
using System.Text;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace MinecraftTunnel
{
    public class Program
    {
        private static IHost host;

        public static ushort MaxConnections;
        public static bool WhiteList;

        public static ConnectConfig ServerConfig;
        public static ConnectConfig NatConfig;
        public static QueryConfig QueryConfig;



        public static string NoFind;
        public static string IsEnd;

        public static string ConnectionString;


        public static readonly ILog log = LogManager.GetLogger("MinecraftTunnel");

        public static IConfigurationRoot Configuration { get; set; }

        public static void Main(string[] args)
        {
            host = CreateHostBuilder(args).Build();
            host.Run();

            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddYamlFile("appsettings.yaml");
            Configuration = builder.Build();

            _ = ushort.TryParse(Configuration["MaxConnections"], out MaxConnections);
            _ = bool.TryParse(Configuration["WhiteList"], out WhiteList);

            ServerConfig = Configuration.GetSection("Server").Get<ConnectConfig>();
            NatConfig = Configuration.GetSection("Nat").Get<ConnectConfig>();
            QueryConfig = Configuration.GetSection("Query").Get<QueryConfig>();

            ConnectionString = Configuration["DataBase:ConnectionString"];

            NoFind = Configuration["Message:NotFind"];
            IsEnd = Configuration["Message:IsEnd"];

            StateContext stateContext = new StateContext(MaxConnections, ushort.MaxValue);
            stateContext.Init();
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, ServerConfig.Port);
            stateContext.Start(serverIP);

            int CursorTop, OnPlayer = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService<LoginService>();
                     services.AddScoped<ServerCore>();
                 })
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     configApp.AddYamlFile("appsettings.json", optional: true);
                     configApp.AddCommandLine(args);
                 })
                 .ConfigureLogging((hostContext, configLogging) =>
                 {
                     configLogging.AddConsole();
                     configLogging.AddDebug();
                 })
                 .UseConsoleLifetime();
    }
}
