using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol;
using System;
using System.IO;
using System.Net.Sockets;

namespace MinecraftTunnel.Core
{
    public class AsyncUserToken
    {
        public Tunnel tunnel;
        protected byte[] ReceiveBuffer;
        private int BufferSize;

        public Action<object, SocketAsyncEventArgs> IO_Completed;
        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;
        public Socket ServerSocket;
        public bool StartLogin = false;
        public int ProtocolVersion;

        public int TotalBytesRead, TotalBytesSend;
        public string PlayerName;

        public DateTime ConnectDateTime;    // 连接时间
        public DateTime EndTime;            // 到期时间
        public bool IsForge;
        public bool IsCompression = false;

        public AsyncUserToken(int ReceiveBufferSize)
        {
            this.BufferSize = ReceiveBufferSize;
            ReceiveBuffer = new byte[BufferSize];
            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.UserToken = this;
            ReceiveEventArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.UserToken = this;
        }

        public void Send(byte[] buffer)
        {
            SendEventArgs.SetBuffer(buffer);
            ServerSocket.SendAsync(SendEventArgs);
        }

        public void Tunnel(StateContext stateContext)
        {
            UnCompleted();
            ConnectDateTime = DateTime.Now;
            tunnel = new Tunnel(Program.NatConfig.IP, Program.NatConfig.Port, BufferSize);
            tunnel.Bind(stateContext, this);
            SetComplete(tunnel.IO_Completed);
            Completed();
        }

        #region Complete
        public void SetComplete(Action<object, SocketAsyncEventArgs> IO_Completed)
        {
            this.IO_Completed = IO_Completed;
        }
        public void Completed()
        {
            if (IO_Completed != null)
            {
                ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            }
        }
        public void UnCompleted()
        {
            if (IO_Completed != null)
            {
                ReceiveEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                SendEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            }
        }
        #endregion

        public void Close()
        {
            if (StartLogin)
            {
                UnCompleted();
                IO_Completed = null;
                StartLogin = false;
                if (tunnel != null)
                    tunnel.Close();
            }
            IsForge = false;
            ProtocolVersion = 0;
            ServerSocket.Close();
        }

        public override bool Equals(object obj)
        {
            return obj is AsyncUserToken token &&
                   PlayerName == token.PlayerName;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerName);
        }


        public void Kick(Chat chat)
        {
            SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = chat.Pack();
                memoryStream.WriteInt(buffer.Length);
                memoryStream.Write(buffer);
                sendPacket.SetBuffer(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                ServerSocket.SendAsync(sendPacket);
            }

        }
        public void Kick(string message)
        {
            SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();
            using (MemoryStream stream = new MemoryStream())
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.WriteInt(0);
                    memoryStream.WriteString(message, true);

                    stream.WriteInt((int)memoryStream.Position);
                    stream.Write(memoryStream.GetBuffer());

                    sendPacket.SetBuffer(stream.GetBuffer(), 0, (int)stream.Position);
                    ServerSocket.SendAsync(sendPacket);
                }
            }


        }

    }
}