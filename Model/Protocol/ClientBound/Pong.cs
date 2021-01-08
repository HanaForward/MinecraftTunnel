namespace MinecraftTunnel.Model.Protocol.ClientBound
{
    public class Pong
    {
        public long Payload { get; set; }
        public int PacketId => 0;
    }
}
