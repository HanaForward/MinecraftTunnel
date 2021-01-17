using MinecraftTunnel.Common;
using System;

namespace MinecraftTunnel.Protocol
{
    public interface IProtocol
    {
        public bool NeedAnalysis { get; set; }
        public Action<PlayerToken, object> Action { get; set; }
    }
}