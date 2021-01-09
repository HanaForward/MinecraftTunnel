using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Core;
using MinecraftTunnel.Model;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Service;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace MinecraftTunnel
{
    public class Program
    {
        private static IHost host;
        public static readonly ILog log = LogManager.GetLogger("MinecraftTunnel");




        public static ushort MaxConnections;
        public static bool WhiteList;
        public static ConnectConfig ServerConfig;
        public static ConnectConfig NatConfig;
        public static QueryConfig QueryConfig;
        public static string NoFind;
        public static string IsEnd;
        public static string ConnectionString;



        public static IConfigurationRoot Configuration { get; set; }
        public static void Main(string[] args)
        {
            Block block = new Block(new byte[10]);
            var readString = typeof(Block).GetMethod("readInt", BindingFlags.Instance | BindingFlags.Public);
            readString.Invoke(block, null);



            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            host = CreateHostBuilder(args).Build();
            host.Run();
            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddHostedService<LoginService>();
                     services.AddScoped<ServerCore>();



                     services.AddSingleton(o =>
                     {
                         return new TotalService();
                     });
                     services.AddSingleton(o =>
                     {
                         var log = (ILogger<AnalysisService>)o.GetService(typeof(ILogger<AnalysisService>));
                         return new AnalysisService(log);
                     });
                 })
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.yaml");
                     configApp.AddYamlFile(path, optional: true);
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
