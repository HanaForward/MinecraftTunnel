using System;
using System.Net.Sockets;

namespace MinecraftTunnel.Common
{
    public class PlayerToken
    {
        public Socket ServerSocket;

        public SocketAsyncEventArgs ReceiveEventArgs;
        public SocketAsyncEventArgs SendEventArgs;

        private Action<SocketAsyncEventArgs> ProcessReceive;
        private Action<SocketAsyncEventArgs> ProcessSend;

        protected byte[] ReceiveBuffer;
        public string PlayerName;                            // 玩家Name
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

            ReceiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            SendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }

        /// <summary>
        /// 每当套接字上完成接收或发送操作时，都会调用此方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">与完成的接收操作关联的SocketAsyncEventArg</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive?.Invoke(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend?.Invoke(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        public void SetCompleted(Action<SocketAsyncEventArgs> ProcessReceive, Action<SocketAsyncEventArgs> ProcessSend)
        {
            this.ProcessReceive = ProcessReceive;
            this.ProcessSend = ProcessSend;
        }

        public void Close()
        {
            /*
                    Interlocked.Decrement(ref ConnectedSockets);
                    if (null == token.IO_Completed)
                    {
                        token.SetComplete(IO_Completed);
                        token.Completed();
                    }
                    if (token.PlayerName != null)
                    {
                        Online.Remove(token.PlayerName);
                        token.PlayerName = null;
                    }
                    */
        }
    }
}
