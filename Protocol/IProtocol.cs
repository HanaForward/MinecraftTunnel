using MinecraftTunnel.Common;
using System;

namespace MinecraftTunnel.Protocol
{
    public interface IProtocol
    {
        public bool NeedAnalysis { get; set; }
        public Func<PlayerToken, object, bool> Action { get; set; }
    }
}