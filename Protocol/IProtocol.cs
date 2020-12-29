namespace MinecraftTunnel.Protocol
{
    public interface IProtocol
    {
        int PacketId { get; }
        void Analyze(Block block);
        byte[] Pack();
    }
}
