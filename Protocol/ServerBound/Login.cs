using MinecraftTunnel.Extensions;
using System.IO;

namespace MinecraftTunnel.Protocol.ServerBound
{
    public class Login : IProtocol
    {
        public string Name;
        public int PacketId { get; } = 0;
        public void Analyze(Block block)
        {
            int Leng = block.readVarInt();
            Name = block.readString(Leng);
        }

        public byte[] Pack()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.WriteInt(PacketId);
                memoryStream.WriteString(Name, true);
                return memoryStream.ToArray();
            }
        }
    }
}
