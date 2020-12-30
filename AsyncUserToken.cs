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
        public void Tunnel(StateContext stateContext)
        {
            ConnectDateTime = DateTime.Now;
            UnCompleted();
            SetComplete(null);

            tunnel = new Tunnel(Program.NatConfig.IP, Program.NatConfig.Port);
            tunnel.Bind(stateContext, this);

            SetComplete(tunnel.IO_Completed);
            Completed();
        }

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

        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;

        public Socket ServerSocket; 

        public bool StartLogin = false;
        public int ProtocolVersion;

        public void Close()
        {
            if (StartLogin)
            {
                StartLogin = false;
                tunnel.Close();
            }
            ProtocolVersion = 0;
            ServerSocket.Close();
        }
    }
}