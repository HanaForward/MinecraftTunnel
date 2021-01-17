using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Core;
using MinecraftTunnel.Service;
using MinecraftTunnel.Service.ProtocolService;
using System;
using System.IO;
using System.Text;

namespace MinecraftTunnel
{
    public class Program
    {
        private static IHost host;
        public static IConfigurationRoot Configuration { get; set; }
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            host = CreateHostBuilder(args).Build();
            host.Run();
            Console.ReadLine();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureServices((hostContext, services) =>
                 {
                     services.AddSingleton<ServerListen>();
                     services.AddSingleton<LoginService>();
                     services.AddSingleton<SemaphoreService>();
                     services.AddSingleton<CompressionService>();

                     services.AddHostedService<TunnelService>();


                     services.AddScoped<ServerCore>();


                     services.AddSingleton(o =>
                     {
                         return new TotalService();
                     });
                     services.AddSingleton(o =>
                     {
                         var log = (ILogger<AnalysisService>)o.GetService(typeof(ILogger<AnalysisService>));
                         return new AnalysisService(o, log);
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
