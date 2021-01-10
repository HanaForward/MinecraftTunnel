using System;
using System.Net.Sockets;

namespace MinecraftTunnel.Common
{
    public class PlayerToken
    {
        public Socket ServerSocket;
        public Socket ClientSocket;

        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;

        protected byte[] ReceiveBuffer;

        public bool StartLogin;
        public bool Compression;
        public string PlayerName;                            // 玩家Name
        public int ProtocolVersion;


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

        public void Close()
        {
            try
            {
                ServerSocket.Shutdown(SocketShutdown.Both);
                ServerSocket.Close();
            }
            catch (Exception)
            {

            }
        }
    }
}
