namespace MinecraftTunnel.Protocol
{
    /// <summary>
    /// 任何一个传输协议处理过程需要继承本方法
    /// </summary>
    public abstract class IAnalyzeProtocol
    {
        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="PacketData"></param>
        public abstract void Analyze(byte[] PacketData);
        /// <summary>
        /// 逆解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ProtocolModel"></param>
        /// <returns></returns>
        public abstract byte[] Resolve<T>(T ProtocolModel);
    }
}