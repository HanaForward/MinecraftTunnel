namespace MinecraftTunnel.Protocol.ServerBound
{
    public class Handshake
    {
        /// <summary>
        /// 协议版本
        /// </summary>
        public int ProtocolVersion { get; set; }
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
        public NextState NextState { get; set; }
        public int PacketId => 0;
    }
    public enum NextState
    {
        status = 1,
        login = 2
    }
}
