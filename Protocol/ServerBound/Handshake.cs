using MinecraftTunnel.Extensions;
using System.IO;

namespace MinecraftTunnel.Protocol.ServerBound
{
    public class Handshake : IProtocol
    {
        /// <summary>
        /// 协议版本
        /// </summary>
        public int ProtocolVersion;
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress;
        /// <summary>
        /// 服务器端口
        /// </summary>
        public ushort ServerPort;
        /// <summary>
        /// 下一步状态
        /// </summary>
        public NextState NextState;
        public int PacketId { get; private set; } = 0;
        public void Analyze(Block block)
        {
            ProtocolVersion = block.readVarInt();
            int ServerAddressLength = block.readVarInt();
            ServerAddress = block.readString(ServerAddressLength);
            ServerPort = block.readShort();
            NextState = (NextState)block.readVarInt();
        }
        public byte[] Pack()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.WriteInt(PacketId);
                memoryStream.WriteInt(ProtocolVersion);
                memoryStream.WriteString(ServerAddress, true);
                memoryStream.WriteUShort(ServerPort);
                memoryStream.WriteInt((int)NextState);
                return memoryStream.ToArray();
            }
        }
    }
    public enum NextState
    {
        status = 1,
        login = 2
    }
}
