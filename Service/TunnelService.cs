using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Core;
using MinecraftTunnel.Protocol;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace MinecraftTunnel.Service
{
    /// <summary>
    /// 当服务器接受到玩家数据包
    /// </summary>
    /// <param name="PlayerToken">连接标识</param>
    /// <param name="Packet">数据包</param>
    public delegate void OnReceive(PlayerToken PlayerToken, byte[] Packet);
    /// <summary>
    /// 当服务器发送给玩家数据包
    /// </summary>
    /// <param name="PlayerToken">连接标识</param>
    /// <param name="Packet">数据包</param>
    public delegate void OnSend(PlayerToken PlayerToken, byte[] Packet);
    /// <summary>
    /// 当玩家、或转发器断开连接
    /// </summary>
    /// <param name="playerToken">连接标识</param>
    public delegate void OnClose(PlayerToken playerToken);

    public class TunnelService : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        private readonly AnalysisService AnalysisService;


        public readonly ServerCore ServerCore;

        public delegate void OnPlayerConnet(PlayerToken PlayerToken);



        public TunnelService(ILogger<LoginService> Logger, IConfiguration Configuration, AnalysisService AnalysisService, ServerCore ServerCore)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.AnalysisService = AnalysisService;
            this.ServerCore = ServerCore;

            ServerCore.OnReceive += Even_OnReceive;
            ServerCore.OnSend += Even_OnSend;
        }

        private void Even_OnSend(PlayerToken PlayerToken, byte[] Packet)
        {
            ProtocolHeand protocolHeand = AnalysisService.AnalysisHeand(Packet);
            Logger.LogInformation($"OnSend  -> PacketId : {protocolHeand.PacketId} , Size : {protocolHeand.PacketSize} , Data : {protocolHeand.PacketData}");
        }

        private void Even_OnReceive(PlayerToken PlayerToken, byte[] Packet)
        {

            Logger.LogInformation($"OnReceive -> {PacketId} , Size : {PacketData.Length} , Data : {PacketData}");
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ServerCore.Start(Configuration["Server:IP"], Configuration.GetValue<int>("Server:Port"));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ServerCore.Stop();
            return Task.CompletedTask;
        }
    }
}
