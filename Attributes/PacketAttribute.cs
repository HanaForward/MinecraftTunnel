using System;

namespace MinecraftTunnel.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketAttribute : Attribute
    {
        public int PakcetId { get; set; }
    }
}
