using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Protocol;
using System;
using System.Collections.Generic;
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
        public async Task<object> AnalysisData(int PacketId, byte[] PacketData)
        {
            Block block = new Block(PacketData);
            return EntityMapper.MapToEntities(PacketId, block);
        }

        public List<ProtocolHeand> AnalysisHeand(bool Compression, byte[] Packet)
        {
            Block block = new Block(Packet);
            List<ProtocolHeand> protocolHeands = new List<ProtocolHeand>();

            ProtocolHeand protocolHeand;
            // 根据玩家的 Compression 标志 来处理数据包
            if (Compression)
            {
                do
                {
                    protocolHeand = new ZipProtocol();
                    protocolHeand.Analyze(block);
                    protocolHeands.Add(protocolHeand);
                } while (protocolHeand.block.step < Packet.Length);
            }
            else
            {
                do
                {
                    protocolHeand = new NormalProtocol();
                    protocolHeand.Analyze(block);
                    protocolHeands.Add(protocolHeand);
                } while (protocolHeand.block.step < Packet.Length);
            }
            return protocolHeands;
        }
    }
}
