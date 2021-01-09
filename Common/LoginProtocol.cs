using MinecraftTunnel.Attributes;
using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MinecraftTunnel.Common
{
    [ProtocolService(PakcetId = 0)]
    public class LoginProtocol : IProtocol
    {
        public override void Analyze(PlayerToken playerToken, byte[] PacketData)
        {
            Block block = new Block(PacketData);
            if (playerToken.StartLogin)
            {
                // 开启隧道
                playerToken.PlayerName = block.readString(16);
                playerToken.Tunnel();
            }
            if (PacketData.Length > 3)
            {
                // 登记状态
                block = new Block(PacketData);
                Handshake handshake = EntityMapper.MapToEntities<Handshake>(block);
                playerToken.ProtocolVersion = handshake.ProtocolVersion;
                if (handshake.NextState == NextState.login)
                {
                    playerToken.StartLogin = true;
                }
            }
            else
            {

                Response response = new Response("1.8.9", playerToken.ProtocolVersion);
                response.players.online =  0;
                response.players.max = Program.MaxConnections;
                response.players.sample = new List<SampleItem>();
                response.description.text = "新版本测试";
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