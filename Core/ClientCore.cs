using MinecraftTunnel.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MinecraftTunnel.Core
{
    public class ClientCore : TunnelCore
    {
        private readonly PlayerToken playerToken;

        public Socket ClientSocket;
        private SocketAsyncEventArgsPool SocketAsync;
        private byte[] ReceiveBuffer;
        private Mutex mutex = new Mutex();

        public ClientCore(PlayerToken playerToken)
        {
            this.playerToken = playerToken;

            playerToken.SetCompleted(Relay, null);
        }
        public override void Start(string IP, int Port)
        {
            SocketAsync = new SocketAsyncEventArgsPool(10);
            ReceiveBuffer = new byte[ushort.MaxValue];

            IPAddress ipaddr;
            if (!IPAddress.TryParse(IP, out ipaddr))
            {
                IPAddress[] iplist = Dns.GetHostAddresses(IP);
                if (iplist != null && iplist.Length > 0)
                {
                    ipaddr = iplist[0];
                }
            }
            IPEndPoint localEndPoint = new IPEndPoint(ipaddr, Port);
            ClientSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ClientSocket.NoDelay = true;

            ClientSocket.Connect(localEndPoint);


            SocketAsyncEventArgs socketAsync = new SocketAsyncEventArgs();
            socketAsync.RemoteEndPoint = localEndPoint;
            socketAsync.Completed += IO_Completed;

            return;

            if (!ClientSocket.ConnectAsync(socketAsync))
            {
                ProcessConnect(socketAsync);
            }
        }

        /// <summary>
        /// Completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="socketAsync"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs socketAsync)
        {
            switch (socketAsync.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(socketAsync);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(socketAsync);
                    break;
                case SocketAsyncOperation.Connect:
                    ProcessConnect(socketAsync);
                    break;
                default:
                    throw new ArgumentException("套接字上完成的最后一个操作不是接收或发送或连接。");
            }
        }

        /// <summary>
        /// 连接回调事件
        /// </summary>
        /// <param name="socketAsync"></param>
        private void ProcessConnect(SocketAsyncEventArgs socketAsync)
        {
            if (socketAsync.SocketError == SocketError.Success)
            {
                socketAsync = new SocketAsyncEventArgs();
                socketAsync.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
                socketAsync.Completed += IO_Completed;

                if (!ClientSocket.ReceiveAsync(socketAsync))
                {
                    ProcessReceive(socketAsync);
                }
            }
        }

        private void ProcessSend(SocketAsyncEventArgs socketAsync)
        {
            if (socketAsync.SocketError == SocketError.Success)
            {
                SocketAsync.Push(socketAsync);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs socketAsync)
        {
            if (socketAsync.BytesTransferred > 0 && socketAsync.SocketError == SocketError.Success)
            {
                byte[] data = new byte[socketAsync.BytesTransferred];
                Buffer.BlockCopy(socketAsync.Buffer, socketAsync.Offset, data, 0, socketAsync.BytesTransferred);
                if (ClientSocket.Connected == true)
                {
                    /*
                     * 此处完成转发
                     */
                    playerToken.SendEventArgs.SetBuffer(data);
                    playerToken.ServerSocket.SendAsync(playerToken.SendEventArgs);
                    if (!ClientSocket.ReceiveAsync(socketAsync))
                    {
                        ProcessReceive(socketAsync);
                    }
                }
            }
            else
            {
                playerToken.Close();
            }
        }


        public void Relay(SocketAsyncEventArgs socketAsyncEvent)
        {
            int offset = playerToken.ReceiveEventArgs.Offset;
            int count = playerToken.ReceiveEventArgs.BytesTransferred;
            int endOffset = offset + count;
            byte[] Buffer = playerToken.ReceiveEventArgs.Buffer;
            if (count > 0 && playerToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {

                mutex.WaitOne();
                //如果发送池为空时，临时新建一个放入池中
                if (SocketAsync.Count == 0)
                {
                    SocketAsyncEventArgs saea_send = new SocketAsyncEventArgs();
                    saea_send.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                    SocketAsync.Push(saea_send);
                }
                SocketAsyncEventArgs sendEventArgs = SocketAsync.Pop();
                mutex.ReleaseMutex();

                sendEventArgs.SetBuffer(Buffer);
                if (!ClientSocket.SendAsync(sendEventArgs))
                {
                    ProcessSend(sendEventArgs);
                }
            }
            else
            {
                playerToken.Close();
            }
        }

        public override void Stop()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }
        public override void Dispose()
        {
            Stop();
            ClientSocket.Dispose();
        }
    }
}
