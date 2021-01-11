using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Core;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Service.ProtocolService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MinecraftTunnel.Service
{
    /// <summary>
    /// 当服务器接受到玩家数据包
    /// </summary>
    /// <param name="PlayerToken">连接标识</param>
    /// <param name="Packet">数据包</param>
    public delegate void ServerReceive(PlayerToken PlayerToken, byte[] Packet);
    /// <summary>
    /// 当服务器发送给玩家数据包
    /// </summary>
    /// <param name="PlayerToken">连接标识</param>
    /// <param name="Packet">数据包</param>
    public delegate void ServerSend(PlayerToken PlayerToken, byte[] Packet);
    /// <summary>
    /// 当收到服务器回复的数据包
    /// </summary>
    /// <param name="playerToken"></param>
    /// <param name="Packet"></param>
    public delegate void TunnelReceive(PlayerToken playerToken, byte[] Packet);
    /// <summary>
    /// 当收到服务器发送的数据包
    /// </summary>
    /// <param name="playerToken"></param>
    /// <param name="Packet"></param>
    public delegate void TunnelSend(PlayerToken playerToken, byte[] Packet);
    /// <summary>
    /// 当玩家断开连接
    /// </summary>
    /// <param name="playerToken">连接标识</param>
    public delegate void OnClose(PlayerToken playerToken);

    public class TunnelService : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        private readonly AnalysisService AnalysisService;

        public readonly ServerCore ServerCore;

        private Dictionary<int, Type> Collections = new Dictionary<int, Type>();
        public Dictionary<int, IProtocol<object>> ProtocalAction = new Dictionary<int, IProtocol<object>>();

        public TunnelService(IServiceProvider ServiceProvider, ILogger<TunnelService> Logger, IConfiguration Configuration, AnalysisService AnalysisService, ServerCore ServerCore)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.AnalysisService = AnalysisService;
            this.ServerCore = ServerCore;

            ServerCore.OnClose += Even_OnClose;
            ClientCore.OnClose += Even_OnClose;

            ServerCore.OnServerReceive += Even_OnServerReceive;
            ServerCore.OnServerSend += Even_OnServerSend;
            ClientCore.OnTunnelReceive += Even_TunnelReceive;
            ClientCore.OnTunnelSend += Even_OnTunnelSend;

            Collections.Add(0, typeof(LoginService));

            foreach (var item in Collections)
            {
                Type type = item.Value;
                ProtocalAction.Add(item.Key, (IProtocol<object>)ServiceProvider.GetService(type));
            }
        }

        /// <summary>
        /// 连接断开
        /// </summary>
        /// <param name="playerToken"></param>
        private void Even_OnClose(PlayerToken playerToken)
        {
            if (playerToken.ServerCore != null)
            {
                playerToken.CloseServer();
            }
            if (playerToken.ClientCore != null)
            {
                playerToken.CloseClient();
            }
        }
        private void Even_OnServerSend(PlayerToken PlayerToken, byte[] Packet)
        {
            List<ProtocolHeand> protocolHeands = AnalysisService.AnalysisHeand(PlayerToken.Compression, Packet);
            foreach (var protocolHeand in protocolHeands)
            {
                Logger.LogInformation($"ServerSend  -> PacketId : {protocolHeand.PacketId} , Size : {protocolHeand.PacketSize} , PacketData : {protocolHeand.PacketData}");
            }
        }
        private void Even_OnServerReceive(PlayerToken PlayerToken, byte[] Packet)
        {
            List<ProtocolHeand> protocolHeands = AnalysisService.AnalysisHeand(PlayerToken.Compression, Packet);
            foreach (var protocolHeand in protocolHeands)
            {
                if (ProtocalAction.TryGetValue(protocolHeand.PacketId, out IProtocol<object> ActionProtocol))
                {
                    object model = protocolHeand.PacketData;
                    if (ActionProtocol.NeedAnalysis)
                        model = AnalysisService.AnalysisData<object>(protocolHeand.PacketId, protocolHeand.PacketData);
                    ActionProtocol.Action?.Invoke(PlayerToken, model);
                }
                Logger.LogInformation($"ServerReceive  -> PacketId : {protocolHeand.PacketId} , Size : {protocolHeand.PacketSize} , PacketData : {protocolHeand.PacketData}");
            }
        }
        private void Even_TunnelReceive(PlayerToken playerToken, byte[] Packet)
        {
            Logger.LogInformation($"TunnelReceive  ->  Length : {Packet.Length}");
        }
        private void Even_OnTunnelSend(PlayerToken playerToken, byte[] Packet)
        {
            Logger.LogInformation($"TunnelSend  ->  Length : {Packet.Length}");
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
