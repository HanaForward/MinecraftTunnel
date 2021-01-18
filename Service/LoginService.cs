using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Common;
using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MinecraftTunnel.Service
{
    public class LoginService : IProtocol
    {
        private readonly ILogger Logger;
        private readonly TotalService TotalService;
        private readonly IConfiguration Configuration;

        public LoginService(ILogger<LoginService> Logger, IConfiguration Configuration, TotalService TotalService)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.TotalService = TotalService;
            this.Action = Auth;


            EntityMapper.PacketType.Add(Handshake.PacketId, typeof(Handshake));
        }

        public object Instance { get; set; }
        public Func<PlayerToken, object, bool> Action { get; set; }
        public bool NeedAnalysis { get; set; } = false;
        private bool Auth(PlayerToken playerToken, object obj)
        {
            try
            {
                byte[] buffer = (byte[])obj;
                if (buffer.Length == 0)
                {
                    Response response = new Response("1.8.9", playerToken.ProtocolVersion);
                    response.players.online = TotalService.TotalPlayer;
                    response.players.max = 0;
                    response.players.sample = new List<SampleItem>();
                    response.description.text = "新版本测试";
                    response.favicon = "";
                    using (River temp = new River())
                    {
                        temp.WriteInt(0);
                        temp.WriteString(JsonSerializer.Serialize(response), true);
                        byte[] packet = temp.GetBytes();
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            memoryStream.WriteInt(packet.Length);
                            memoryStream.Write(packet);
                            playerToken.ServerCore.SendPacket(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                        }
                    }
                    return false;
                }
                Block block = new Block(buffer);
                if (playerToken.StartLogin)
                {
                    playerToken.StartTunnel();
                    playerToken.PlayerName = block.readString();
                    ushort.TryParse(Configuration["ServerAddress"], out ushort ServerPort);
                    playerToken.Login(Configuration["Query:ServerAddress"], 25565);
                    return false;
                }
                Handshake handshake = (Handshake)EntityMapper.MapToEntities(Handshake.PacketId, block);
                if (handshake.NextState() == NextState.login)
                {
                    playerToken.ProtocolVersion = handshake.ProtocolVersion;
                    if (handshake.ServerAddress.IndexOf("FML") > 0)
                    {
                        playerToken.IsForge = true;
                    }
                    playerToken.StartLogin = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                return false;
            }
            return false;
        }
    }
}
