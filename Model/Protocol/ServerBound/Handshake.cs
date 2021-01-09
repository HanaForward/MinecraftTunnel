using MinecraftTunnel.Extensions;

namespace MinecraftTunnel.Protocol.ServerBound
{
    public class Handshake
    {
        /// <summary>
        /// 协议版本
        /// </summary>
        public VarInt ProtocolVersion { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerAddress { get; set; }
        /// <summary>
        /// 服务器端口
        /// </summary>
        public ushort ServerPort { get; set; }
        /// <summary>
        /// 下一步状态
        /// </summary>
        public VarInt nextState { get; set; }

        public NextState NextState;

        public int PacketId = 0;

        public Handshake()
        {

        }
    }
    public enum NextState
    {
        status = 1,
        login = 2
    }
}
