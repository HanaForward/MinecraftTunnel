namespace MinecraftTunnel.Protocol
{
    public abstract class ProtocolHeand
    {
        public Block block;

        public int PacketSize;
        public int PacketId;
        public byte[] PacketData;
        public abstract void Analyze(Block block);
    }
}