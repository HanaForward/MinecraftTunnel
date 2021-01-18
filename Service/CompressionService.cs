using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Model.Protocol.ServerBound;
using MinecraftTunnel.Protocol;
using System;

namespace MinecraftTunnel.Service
{
    public class CompressionService : IProtocol
    {
        private readonly ILogger Logger;
        private readonly TotalService TotalService;
        private readonly IConfiguration Configuration;
        public CompressionService(ILogger<CompressionService> Logger, IConfiguration Configuration, TotalService TotalService)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.TotalService = TotalService;
            this.Action = Auth;
        }
        public SetCompression Instance { get; set; }
        public bool NeedAnalysis { get; set; } = false;
        public Action<PlayerToken, SetCompression> Action { get; set; }
        public void Compression(PlayerToken playerToken, SetCompression setCompression)
        {
            playerToken.Compression = true;

            playerToken.Threshold = setCompression.Threshold;
        }
    }
}