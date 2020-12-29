namespace MinecraftTunnel.Protocol.ClientBound
{
    public class Pong : IProtocol
    {
        public long Payload;

        public int PacketId => throw new System.NotImplementedException();

        public void Analyze(Block block)
        {
            Payload = block.readLong();
        }

        public byte[] Pack()
        {
            throw new System.NotImplementedException();
        }
    }
}
