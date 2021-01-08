namespace MinecraftTunnel.Protocol
{
    public class NormalProtocol
    {
        public Block block;
        public int PacketSize;
        public int PacketId;
        public byte[] PacketData;
        public NormalProtocol() { }
        public void Analyze(Block block)
        {
            this.block = block;
            this.PacketSize = block.readVarInt();
            this.PacketId = block.readVarInt();
            this.PacketData = block.readData(PacketSize);
        }
    }
}
