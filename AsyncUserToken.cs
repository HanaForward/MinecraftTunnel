using System;
using System.Net.Sockets;

namespace MinecraftTunnel
{
    public class AsyncUserToken
    {
        public Tunnel tunnel;
        protected byte[] ReceiveBuffer;
        private int BufferSize;
        protected DateTime ConnectDateTime;// 连接时间
        public Action<object, SocketAsyncEventArgs> IO_Completed;
        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;
        public Socket ServerSocket;
        public bool StartLogin = false;
        public int ProtocolVersion;

        private int TotalBytesRead, TotalBytesSend;
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
                tunnel.Close();
            }
            ProtocolVersion = 0;
            ServerSocket.Close();
        }
    }
}