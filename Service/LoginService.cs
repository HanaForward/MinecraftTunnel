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
        private readonly ServerCore ServerCore;


        private static ServerConfig ServerConfig;
        public LoginService(ILogger<LoginService> ILogger, IConfiguration IConfiguration, ServerCore ServerCore)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;
            this.ServerCore = ServerCore;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            ServerConfig = IConfiguration.GetSection("Server").Get<ServerConfig>();
            ServerCore.Bind(ServerConfig.IP, ServerConfig.Port);
            ServerCore.Start();


            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            ServerCore.Stop();
            return Task.CompletedTask;
        }
        public void Dispose()
        {
            ServerCore.Dispose();
        }
    }
}
