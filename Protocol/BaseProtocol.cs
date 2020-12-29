using System;

namespace MinecraftTunnel.Protocol
{
    public class BaseProtocol
    {
        private Block block;

        public int PacketSize;
        public int PacketId;
        public BaseProtocol()
        {
            
        }
        public void Analyze(Block block)
        {
            this.block = block;
            this.PacketSize = block.readVarInt();
            this.PacketId = block.readVarInt();
        }
        public T Resolve<T>() where T : IProtocol, new()
        {
            T PacketData = Activator.CreateInstance<T>();
            PacketData.Analyze(block);
            return PacketData;
        }
    }
}
