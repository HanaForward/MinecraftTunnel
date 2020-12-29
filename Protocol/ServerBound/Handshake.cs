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

        public void Analyze(Block block)
        {
            ProtocolVersion = block.readVarInt();
            int ServerAddressLength = block.readVarInt();
            ServerAddress = block.readString(ServerAddressLength);
            ServerPort = block.readShort();
            NextState = (NextState)block.readVarInt();
        }


    }
    public enum NextState
    {
        status = 1,
        login = 2
    }
}
