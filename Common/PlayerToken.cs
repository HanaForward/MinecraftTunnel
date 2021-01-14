using MinecraftTunnel.Core;
using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.IO;
using System.Net.Sockets;

namespace MinecraftTunnel.Common
{
    public class PlayerToken
    {
        public Socket ServerSocket;
        public Socket ClientSocket;

        public ServerCore ServerCore;
        public ClientCore ClientCore;

        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;

        protected byte[] ReceiveBuffer;

        public bool StartLogin;
        public bool Compression;
        public int Threshold;
        public string PlayerName;                            // 玩家Name
        public int ProtocolVersion;
        public bool IsForge;

        public DateTime ConnectDateTime;                     // 连接时间
        public DateTime EndTime;                             // 到期时间


        public PlayerToken()
        {
            ReceiveBuffer = new byte[ushort.MaxValue];
            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.UserToken = this;
            ReceiveEventArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);

            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.UserToken = this;
        }

        public void StartTunnel()
        {
            ClientCore = new ClientCore(ServerCore.ILogger, ServerCore.IConfiguration);
            ClientCore.Start(this);
        }

        public void Login(string ServerAddress, ushort ServerPort)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Handshake handshake = new Handshake();
                memoryStream.WriteInt(0);
                memoryStream.WriteInt(handshake.PacketId);
                memoryStream.WriteInt(ProtocolVersion);
                memoryStream.WriteString(ServerAddress, true);
                memoryStream.WriteUShort(ServerPort);
                memoryStream.WriteInt(handshake.nextState);
                int size = (int)memoryStream.Position - 4;
                memoryStream.Position = 0;
                memoryStream.WriteInt(size);
                byte[] buffer = new byte[size];
                Array.Copy(memoryStream.GetBuffer(), 0, buffer, 0, size);
                ClientCore.ClientSocket.Send(buffer);
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Login login = new Login();
                login.Name = PlayerName;

                memoryStream.WriteInt(0);
                memoryStream.WriteInt(login.PacketId);
                memoryStream.WriteString(PlayerName, true);
                int size = (int)memoryStream.Position - 4;
                memoryStream.Position = 0;
                memoryStream.WriteInt(size);
                byte[] buffer = new byte[size];
                Array.Copy(memoryStream.GetBuffer(), 0, buffer, 0, size);
                ClientCore.ClientSocket.Send(buffer);
            }

        }

        public void CloseServer()
        {
            ServerCore.Stop();
            ServerSocket = null;
            ServerCore = null;
        }
        public void CloseClient()
        {
            ClientCore.Stop();
            ClientSocket = null;
            ClientCore = null;
        }
    }
}
