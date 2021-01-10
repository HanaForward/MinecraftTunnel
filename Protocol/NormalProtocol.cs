namespace MinecraftTunnel.Protocol
{
    public class NormalProtocol : ProtocolHeand
    {
        public new Block block;
        public new int PacketSize;              // 数据包大小
        public new int PacketId;                // 数据包Id
        public new byte[] PacketData;           // 数据包荷载数据
        public NormalProtocol() { }

        public override void Analyze(Block block)
        {
            this.block = block;
            this.PacketSize = block.readVarInt();
            this.PacketId = block.readVarInt();
            this.PacketData = block.readPacket(PacketSize);
        }
    }
}
