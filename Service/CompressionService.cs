using MinecraftTunnel.Common;
using MinecraftTunnel.Model.Protocol.ServerBound;
using MinecraftTunnel.Protocol;
using System;

namespace MinecraftTunnel.Service
{
    public class CompressionService : IProtocol<SetCompression>
    {
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