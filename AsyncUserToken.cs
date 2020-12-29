using System;
using System.Net.Sockets;

namespace MinecraftTunnel
{
    public class AsyncUserToken
    {
        public Tunnel tunnel;
        protected byte[] m_asyncReceiveBuffer;
        protected DateTime ConnectDateTime;// 连接时间
        private Action<object, SocketAsyncEventArgs> IO_Completed;
        public AsyncUserToken(int ReceiveBufferSize)
        {
            m_asyncReceiveBuffer = new byte[ReceiveBufferSize];

            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.UserToken = this;
            ReceiveEventArgs.SetBuffer(m_asyncReceiveBuffer, 0, m_asyncReceiveBuffer.Length);
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.UserToken = this;
        }
        public void Tunnel()
        {
            ConnectDateTime = DateTime.Now;
            UnCompleted();
            tunnel = new Tunnel("172.65.234.205", 25565);
            tunnel.Bind(this);
            Completed(tunnel.IO_Completed);
        }
        public void Completed(Action<object, SocketAsyncEventArgs> IO_Completed)
        {
            this.IO_Completed = IO_Completed;
            ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }
        public void UnCompleted()
        {
            ReceiveEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            SendEventArgs.Completed -= new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }

        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;

        public Socket Client;
        internal bool StartLogin;

        public void Close()
        {
            Client.Close();
            if (tunnel != null)
                tunnel.Clost();
            tunnel = null;
        }
    }
}