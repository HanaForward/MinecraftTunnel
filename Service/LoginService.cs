using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Protocol;
using System;

namespace MinecraftTunnel.Service
{
    public class LoginService : IProtocol<object>
    {
        private readonly ILogger ILogger;
        private readonly IConfiguration IConfiguration;

        public object Instance { get; set; }
        public Action<PlayerToken, object> Action { get; set; }
        public bool NeedAnalysis { get; set; } = false;

        public LoginService(ILogger<TunnelService> ILogger, IConfiguration IConfiguration)
        {
            this.ILogger = ILogger;
            this.IConfiguration = IConfiguration;

            this.Action = Auth;
        }

        private void Auth(PlayerToken playerToken, object obj)
        {
            if (obj == null)
            {


                return;
            }
            byte[] buffer = (byte[])obj;
        }
    }
}
