using MinecraftTunnel.Extensions;

namespace MinecraftTunnel.Model.Protocol.ServerBound
{
    public class SetCompression
    {
        public int PacketId = 3;
        public VarInt Threshold { get; set; }
    }
}
