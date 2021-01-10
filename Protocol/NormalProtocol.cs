namespace MinecraftTunnel.Protocol
{
    public class NormalProtocol : ProtocolHeand
    {
        public override Block block { get; set; }
        public override int PacketSize { get; set; }              // 数据包大小
        public override int PacketId { get; set; }               // 数据包Id
        public override byte[] PacketData { get; set; }           // 数据包荷载数据
        public NormalProtocol() { }

        public override void Analyze(Block block)
        {
            this.block = block;
            this.PacketSize = block.readVarInt();

            int step = block.step;
            this.PacketId = block.readVarInt();
            int data_step = block.step - step;
            int readSize = PacketSize - data_step;
            if (readSize > 0)
                this.PacketData = block.readPacket(readSize);
        }
    }
}
