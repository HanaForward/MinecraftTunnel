using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Protocol;
using System;
using System.Threading.Tasks;

namespace MinecraftTunnel.Service.ProtocolService
{
    public class AnalysisService
    {
        private readonly ILogger Logger;
        public AnalysisService(IServiceProvider serviceProvider, ILogger<AnalysisService> Logger)
        {
            this.Logger = Logger;
        }
        public async Task AnalysisData(PlayerToken playerToken, int PacketId, byte[] PacketData)
        {
  
        }

        public ProtocolHeand AnalysisHeand(bool Compression, byte[] Packet)
        {
            Block block = new Block(Packet);
            ProtocolHeand protocolHeand;
            // 根据玩家的 Compression 标志 来处理数据包
            if (Compression)
            {
                protocolHeand = new ZipProtocol();
                do
                {
                    protocolHeand.Analyze(block);
                } while (protocolHeand.block.step < Packet.Length);
            }
            else
            {
                protocolHeand = new NormalProtocol();
                do
                {
                    protocolHeand.Analyze(block);
                } while (protocolHeand.block.step < Packet.Length);
            }
            return protocolHeand;
        }

    }
}
