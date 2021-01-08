using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinecraftTunnel.Service
{
    public class AnalysisService
    {
        private readonly ILogger ILogger;
        private readonly Dictionary<int, Action<byte[]>> AnalysisFun = new Dictionary<int, Action<byte[]>>();
        public AnalysisService(ILogger<AnalysisService> ILogger)
        {
            this.ILogger = ILogger;
        }
        public Task Analysis(PlayerToken playerToken, int PacketId, byte[] PacketData)
        {
            ILogger.LogInformation($"PacketId : {PacketId} , Size : {PacketData.Length} , Data : {PacketData}");
            if (AnalysisFun.TryGetValue(PacketId, out Action<byte[]> action))
            {
                action?.Invoke(PacketData);
            }
            return Task.CompletedTask;
        }
    }
}