using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Protocol;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinecraftTunnel.Service
{
    public class AnalysisService
    {
        private readonly ILogger ILogger;
        private readonly Dictionary<int, Action<PlayerToken, byte[]>> AnalysisFun = new Dictionary<int, Action<PlayerToken, byte[]>>();
        public AnalysisService(IServiceProvider serviceProvider, ILogger<AnalysisService> ILogger)
        {
            this.ILogger = ILogger;
            IProtocol protocol = serviceProvider.GetService<LoginProtocol>();
            AnalysisFun.Add(0, protocol.Analyze);
        }

        public Task Analysis(PlayerToken playerToken, int PacketId, byte[] PacketData)
        {
            ILogger.LogInformation($"PacketId : {PacketId} , Size : {PacketData.Length} , Data : {PacketData}");
            if (AnalysisFun.TryGetValue(PacketId, out Action<PlayerToken, byte[]> action))
            {
                action?.Invoke(playerToken, PacketData);
            }
            return Task.CompletedTask;
        }

    }
}