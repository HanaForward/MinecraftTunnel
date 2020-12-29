using MinecraftTunnel.Extensions;
using System;
using System.IO;

namespace MinecraftTunnel.Protocol
{
    public class BaseProtocol
    {
        private Block block;

        public int PacketSize;
        public int PacketId;
        public BaseProtocol() { }
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
        public byte[] Pack<T>(T obj) where T : IProtocol, new()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] temp = obj.Pack();
                memoryStream.WriteInt(temp.Length);
                memoryStream.Write(temp);
                return memoryStream.ToArray();
            }
        }
    }
}
