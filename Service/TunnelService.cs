using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Core;
using MinecraftTunnel.Model.Protocol.ServerBound;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Service.ProtocolService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace MinecraftTunnel.Service
{
    #region 事件委托
    /// <summary>
    ///  当玩家进入隧道
    /// </summary>
    /// <param name="PlayerToken"></param>
    public delegate void PlayerJoin(PlayerToken PlayerToken);
    /// <summary>
    /// 当玩家断开连接
    /// </summary>
    /// <param name="playerToken">连接标识</param>
    public delegate void PlayerLeave(PlayerToken playerToken);
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
    #endregion

    public class TunnelService : IHostedService
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        private readonly IServiceProvider ServiceProvider;

        private readonly AnalysisService AnalysisService;
        private readonly TotalService totalService;

        private Dictionary<int, Type> Collections = new Dictionary<int, Type>();
        private Dictionary<int, IProtocol> ProtocalAction = new Dictionary<int, IProtocol>();

        public TunnelService(IServiceProvider ServiceProvider, ILogger<TunnelService> Logger, IConfiguration Configuration, AnalysisService AnalysisService, TotalService totalService)
        {
            this.ServiceProvider = ServiceProvider;
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.AnalysisService = AnalysisService;
            this.totalService = totalService;

            ServerCore.OnClose += Even_OnClose;
            ClientCore.OnClose += Even_OnClose;

            ServerCore.OnServerReceive += Even_OnServerReceive;
            ClientCore.OnTunnelReceive += Even_TunnelReceive;

            Collections.Add(0, typeof(LoginService));
            Collections.Add(3, typeof(CompressionService));

            foreach (var item in Collections)
            {
                Type type = item.Value;
                ProtocalAction.Add(item.Key, (IProtocol)ServiceProvider.GetService(type));
            }
        }

        private void Even_OnServerReceive(PlayerToken playerToken, byte[] Packet)
        {
            if (playerToken.Tunnel)
            {
                playerToken.ClientCore.SendPacket(Packet);
            }
            else
            {
                playerToken.StartTunnel();
                playerToken.ClientCore.SendPacket(Packet);
            }
            List<ProtocolHeand> protocolHeands = AnalysisService.AnalysisHeand(playerToken.Compression, Packet);
            foreach (var protocolHeand in protocolHeands)
            {
                if (ProtocalAction.TryGetValue(protocolHeand.PacketId, out IProtocol ActionProtocol))
                {
                    object model = protocolHeand.PacketData;
                    if (ActionProtocol.NeedAnalysis)
                        model = AnalysisService.AnalysisData<object>(protocolHeand.PacketId, protocolHeand.PacketData);
                    ActionProtocol.Action.Invoke(playerToken, model);
                }
                Logger.LogInformation($"ServerReceive  -> PacketId : {protocolHeand.PacketId} , Size : {protocolHeand.PacketSize} , PacketData : {protocolHeand.PacketData}");
            }
        }
        private void Even_TunnelReceive(PlayerToken playerToken, byte[] Packet)
        {
            playerToken.ServerCore.SendPacket(Packet);
            Logger.LogInformation($"TunnelReceive  ->  Length : {Packet.Length}");
        }
        private void Even_OnClose(PlayerToken playerToken)
        {

        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            ServerListen serverListen = ServiceProvider.GetService<ServerListen>();
            serverListen.Listen();
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
