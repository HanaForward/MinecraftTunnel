using MinecraftTunnel.Common;
using System;

namespace MinecraftTunnel.Protocol
{
    public interface IProtocol<T>
    {
        public T Instance { get; set; }

        public bool NeedAnalysis { get; set; }
        public Action<PlayerToken, T> Action { get; set; }
    }
}