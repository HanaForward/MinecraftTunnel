using System;

namespace MinecraftTunnel.Core
{
    public abstract class TunnelCore : IDisposable
    {
        public abstract void Start();
        public abstract void Stop();
        public abstract void Dispose();
    }
}
