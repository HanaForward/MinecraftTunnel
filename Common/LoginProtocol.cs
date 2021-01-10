using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinecraftTunnel.Attributes;
using MinecraftTunnel.Extensions;
using MinecraftTunnel.Model;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using MinecraftTunnel.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MinecraftTunnel.Common
{
    [ProtocolService(PakcetId = 0)]
    public class LoginProtocol : IProtocol
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        private readonly TotalService totalService;


        private readonly ConnectConfig connectConfig;
        private readonly QueryConfig queryConfig;


        public LoginProtocol(ILogger<LoginProtocol> Logger, IConfiguration Configuration, TotalService totalService)
        {
            this.Logger = Logger;
            this.Configuration = Configuration;
            this.totalService = totalService;


            connectConfig = Configuration.GetSection("Nat").Get<ConnectConfig>();
            queryConfig = Configuration.GetSection("Query").Get<QueryConfig>();
        }
        public override void Analyze(PlayerToken playerToken, byte[] PacketData)
        {
            Block block = new Block(PacketData);
            if (playerToken.StartLogin)
            {
                // 开启隧道
                playerToken.PlayerName = block.readString();
                Logger.LogInformation($"Player : { playerToken.PlayerName} Start Login");
                playerToken.Tunnel(connectConfig.IP, connectConfig.Port);


                Handshake handshake = new Handshake();
                handshake.ProtocolVersion = playerToken.ProtocolVersion;
                if (playerToken.IsForge)
                    handshake.ServerAddress = queryConfig.ServerAddress + "\0FML\0";
                else
                    handshake.ServerAddress = queryConfig.ServerAddress;

                handshake.ServerPort = 25565;
                handshake.nextState = (int)NextState.login;


                byte[] SendBuffer;
                using (MemoryStream BasePacket = new MemoryStream())
                {
                    byte[] packet;
                    using (MemoryStream QueryData = new MemoryStream())
                    {
                        QueryData.WriteInt(handshake.PacketId);
                        QueryData.WriteInt(handshake.ProtocolVersion);
                        QueryData.WriteString(queryConfig.ServerAddress, true);
                        QueryData.WriteUShort(25565);
                        QueryData.WriteInt((int)NextState.login);
                        packet = QueryData.ToArray();
                    }
                    BasePacket.WriteInt(packet.Length);
                    BasePacket.Write(packet);

                    SendBuffer = new byte[BasePacket.Position];
                    Array.Copy(BasePacket.ToArray(), 0, SendBuffer, 0, BasePacket.Position);
                    playerToken.ClientCore.ClientSocket.Send(SendBuffer);
                }

                using (MemoryStream BasePacket = new MemoryStream())
                {
                    byte[] packet;
                    using (MemoryStream NameData = new MemoryStream())
                    {
                        NameData.WriteInt(0);
                        NameData.WriteString(playerToken.PlayerName, true);
                        packet = NameData.ToArray();
                    }
                    BasePacket.WriteInt(packet.Length);
                    BasePacket.Write(packet);

                    SendBuffer = new byte[BasePacket.Position];
                    Array.Copy(BasePacket.ToArray(), 0, SendBuffer, 0, BasePacket.Position);

                    playerToken.ClientCore.ClientSocket.Send(SendBuffer);
                }

            }
            else if (PacketData.Length > 3)
            {
                // 登记状态
                block = new Block(PacketData);
                Handshake handshake = EntityMapper.MapToEntities<Handshake>(block);
                playerToken.ProtocolVersion = handshake.ProtocolVersion;
                var State = handshake.NextState();
                if (State == NextState.login)
                {
                    playerToken.StartLogin = true;
                }
            }
            else
            {
                Response response = new Response("1.8.9", playerToken.ProtocolVersion);
                response.players.online = totalService.TotalPlayer;
                response.players.max = Program.MaxConnections;
                response.players.sample = new List<SampleItem>();
                QueryConfig queryConfig = Configuration.GetSection("Query").Get<QueryConfig>();
                response.description.text = queryConfig.Motd;
                response.favicon = "";

                using (River temp = new River())
                {
                    temp.WriteInt(0);
                    temp.WriteString(JsonSerializer.Serialize(response), true);
                    byte[] buffer = temp.GetBytes();
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        memoryStream.WriteInt(buffer.Length);
                        memoryStream.Write(buffer);
                        playerToken.SendEventArgs.SetBuffer(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                        playerToken.ServerSocket.SendAsync(playerToken.SendEventArgs);
                    }
                }
            }
        }
    }
}