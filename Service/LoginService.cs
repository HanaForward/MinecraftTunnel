using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Core;
using MinecraftTunnel.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftTunnel.Service
{
    public class LoginService : IHostedService, IDisposable
    {
        private readonly ILogger ILogger;
        private readonly IConfiguration IConfiguration;

        
        private static ServerConfig ServerConfig;

        private ServerCore serverCore;
        public LoginService(ILogger<LoginService> ILogger, IConfiguration IConfiguration)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
           
            ServerConfig = IConfiguration.GetSection("Server").Get<ServerConfig>();

            serverCore = new ServerCore();
            serverCore.Bind(ServerConfig.IP, ServerConfig.Port);
            serverCore.Start();


            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            serverCore.Stop();
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            serverCore.Dispose();
        }
    }
}
