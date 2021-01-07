using MinecraftTunnel.Core;
using MinecraftTunnel.Extensions;
using MinecraftTunnel.Model;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;

namespace MinecraftTunnel
{
    public class StateContext
    {
        #region 事件  
        public event Action OnAccept;                                       // 连接成功事件
        public event Action<AsyncUserToken, byte[], int, int> OnReceive;    // 接受数据事件
        public event Action<int, int> OnSend;                               // 发送数据事件
        public event Action OnClose;                                        // 连接关闭事件
        #endregion

        public uint TotalBytesRead, TotalBytesSend;
        public Dictionary<string, AsyncUserToken> Online = new Dictionary<string, AsyncUserToken>();

        private int MaxConnections, BufferSize;
        private Socket ServerSocket;
        private AsyncUserTokenPool TokenPool;

        private int ConnectedSockets;
        Semaphore semaphore;

        private DatabaseManager databaseManager = new DatabaseManager();

        // Create an uninitialized server instance.
        // To start the server listening for connection requests
        // call the Init method followed by Start method
        //
        // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
        // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
        public StateContext(int MaxConnections, int BufferSize)
        {
            TokenPool = new AsyncUserTokenPool(MaxConnections);

            TotalBytesRead = 0;
            ConnectedSockets = 0;
            this.MaxConnections = MaxConnections;
            this.BufferSize = BufferSize;
            // allocate buffers such that the maximum number of sockets can have one outstanding read and
            //write posted to the socket simultaneously
            semaphore = new Semaphore(MaxConnections, MaxConnections);
        }
        public void Init()
        {
            AsyncUserToken userToken;
            for (int i = 0; i < MaxConnections; i++) //按照连接数建立读写对象
            {
                userToken = new AsyncUserToken(BufferSize);
                userToken.SetComplete(IO_Completed);
                userToken.Completed();
                TokenPool.Push(userToken);
            }
        }
        /// <summary>
        /// 启动tcp服务侦听
        /// </summary>       
        /// <param name="port">监听端口</param>
        public void Start(IPEndPoint localEndPoint)
        {
            ServerSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(localEndPoint);
            ServerSocket.Listen(100);
            ServerSocket.NoDelay = true;
            StartAccept(null);
        }
        /// <summary>
        /// 开始接受客户端的连接请求的操作。
        /// </summary>
        /// <param name="acceptEventArg">发布时要使用的上下文对象服务器侦听套接字上的接受操作</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }
            semaphore.WaitOne();
            bool willRaiseEvent = ServerSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }
        /// <summary>
        /// 当异步连接完成时调用此方法
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            // 原子操作,增加一个客户端数量
            Interlocked.Increment(ref ConnectedSockets);
            // 从接受端重用池获取一个新的SocketAsyncEventArgs对象
            AsyncUserToken userToken = TokenPool.Pop();
            userToken.ServerSocket = e.AcceptSocket;
            // 一旦客户机连接，就准备接收。
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(userToken.ReceiveEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(userToken.ReceiveEventArgs);
            }
            if (OnAccept != null)
            {
                OnAccept();
            }
            // 接受第二连接的请求
            StartAccept(e);
        }
        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        /// <summary>
        /// 每当套接字上完成接收或发送操作时，都会调用此方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">与完成的接收操作关联的SocketAsyncEventArg</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        /// <summary>
        /// 接受处理回调
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;

            int offset = userToken.ReceiveEventArgs.Offset;
            int count = userToken.ReceiveEventArgs.BytesTransferred;

            int endOffset = offset + count;

            byte[] Buffer = userToken.ReceiveEventArgs.Buffer;

            if (count > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref TotalBytesRead, (uint)e.BytesTransferred);
                try
                {
                    Block block = new Block(Buffer, offset);
                    BaseProtocol baseProtocol = new BaseProtocol();
                    do
                    {
                        baseProtocol.Analyze(block);
                        if (baseProtocol.PacketId == 0)
                        {
                            if (baseProtocol.PacketSize > 3)
                            {
                                if (userToken.StartLogin)
                                {
                                    Login login = baseProtocol.Resolve<Login>();
                                    if (Program.WhiteList)
                                    {
                                        UserModel userModel = databaseManager.FindPlayer(login.Name);
                                        if (userModel == null)
                                        {
                                            userToken.Kick(Program.NoFind);
                                            break;
                                        }
                                        else
                                        {
                                            if (userModel.End_at < DateTime.Now)
                                            {
                                                userToken.Kick(Program.IsEnd);
                                                break;
                                            }
                                        }
                                        userToken.EndTime = userModel.End_at;
                                    }
                                    userToken.Tunnel(this);
                                    userToken.PlayerName = login.Name;
                                    userToken.tunnel.Login(login.Name, userToken.ProtocolVersion, userToken.IsForge);
                                    Online.Add(userToken.PlayerName, userToken);
                                }
                                else
                                {
                                    Handshake handshake = baseProtocol.Resolve<Handshake>();
                                    userToken.ProtocolVersion = handshake.ProtocolVersion;
                                    if (handshake.NextState == NextState.login)
                                    {
                                        if (handshake.ServerAddress.IndexOf("FML") > 0)
                                        {
                                            userToken.IsForge = true;
                                        }
                                        userToken.StartLogin = true;
                                    }
                                    else
                                    {
                                        userToken.StartLogin = false;
                                    }
                                }
                            }
                            else
                            {
                                // 开始处理本次收到的数据包
                                SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();
                                Response response = new Response("1.8.9", userToken.ProtocolVersion);
                                response.players.online = Online.Count;
                                response.players.max = Program.MaxConnections;
                                response.players.sample = new List<SampleItem>();
                                response.description.text = Program.QueryConfig.Motd;
                                response.favicon = "";

                                using (Block temp = new Block())
                                {
                                    temp.WriteInt(0);
                                    temp.WriteString(JsonSerializer.Serialize(response), true);
                                    byte[] buffer = temp.GetBytes();
                                    using (MemoryStream memoryStream = new MemoryStream())
                                    {
                                        memoryStream.WriteInt(buffer.Length);
                                        memoryStream.Write(buffer);
                                        sendPacket.SetBuffer(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                                        userToken.ServerSocket.SendAsync(sendPacket);
                                    }
                                    // Console.Out.WriteLine("New player.");
                                }
                            }
                        }
                        else if (baseProtocol.PacketId == 1)
                        {
                            SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();
                            Pong pong = baseProtocol.Resolve<Pong>();
                            using (Block temp = new Block())
                            {
                                temp.WriteInt(1);
                                temp.WriteLong(pong.Payload);
                                byte[] buffer = temp.GetBytes();
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    memoryStream.WriteInt(buffer.Length);
                                    memoryStream.Write(buffer);
                                    sendPacket.SetBuffer(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                                }
                                // Console.Out.WriteLine("New Ping.");
                                userToken.ServerSocket.SendAsync(sendPacket);
                            }
                        }
                    } while (baseProtocol.block.step < endOffset);
                }

                catch (Exception ex)
                {


                }
                // 准备下次接收数据      
                bool willRaiseEvent = userToken.ServerSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                    ProcessReceive(userToken.ReceiveEventArgs);
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional
        // data sent from the client
        //
        // <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client
                bool willRaiseEvent = token.ServerSocket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        public void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            try
            {
                token.ServerSocket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            token.Close();
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

            TokenPool.Push(token);
            semaphore.Release();
        }
    }
}

