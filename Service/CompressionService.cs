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
            Action = Compression;
        }
        public SetCompression Instance { get; set; }
        public bool NeedAnalysis { get; set; } = false;
        public Func<PlayerToken, object, bool> Action { get; set; }
        public bool Compression(PlayerToken playerToken, object obj)
        {
            SetCompression setCompression = obj as SetCompression;
            playerToken.Compression = true;
            playerToken.Threshold = setCompression.Threshold;
            return true;
        }
    }
}