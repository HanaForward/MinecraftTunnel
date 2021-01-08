namespace MinecraftTunnel.Protocol
{
    public class BaseProtocol
    {
        public Block block;

        public int PacketSize;
        public int PacketId;
        public byte[] PacketData;
        public BaseProtocol() { }
        public void Analyze(Block block)
        {
            this.block = block;
            this.PacketSize = block.readVarInt();
            this.PacketId = block.readVarInt();
            this.PacketData = block.readData(PacketSize);
        }
    }
}
