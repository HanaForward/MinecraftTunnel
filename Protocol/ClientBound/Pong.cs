namespace MinecraftTunnel.Protocol.ClientBound
{
    public class Pong : IProtocol
    {
        public long Payload;

        public void Analyze(Block block)
        {
            Payload = block.readLong();
        }
    }
}
