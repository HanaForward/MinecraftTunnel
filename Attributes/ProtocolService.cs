using System;

namespace MinecraftTunnel.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProtocolService : Attribute
    {
        public int PakcetId { get; set; }
    }
}
