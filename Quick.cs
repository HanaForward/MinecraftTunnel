using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
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

        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations

        private AsyncUserTokenPool m_asyncSocketUserTokenPool;

        int m_totalBytesRead;           // counter of the total # bytes received by the server

        int m_numConnectedSockets;      // 当前连接数
        Semaphore m_maxNumberAcceptedClients;

        // Create an uninitialized server instance.
        // To start the server listening for connection requests
        // call the Init method followed by Start method
        //
        // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
        // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
        public StateContext(int numConnections, int receiveBufferSize)
        {
            m_asyncSocketUserTokenPool = new AsyncUserTokenPool(numConnections);

            m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            // allocate buffers such that the maximum number of sockets can have one outstanding read and
            //write posted to the socket simultaneously

            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        // Initializes the server by preallocating reusable buffers and
        // context objects.  These objects do not need to be preallocated
        // or reused, but it is done this way to illustrate how the API can
        // easily be used to create reusable objects to increase server performance.
        //
        public void Init()
        {
            AsyncUserToken userToken;
            for (int i = 0; i < m_numConnections; i++) //按照连接数建立读写对象
            {
                userToken = new AsyncUserToken(1024);
                userToken.Completed(IO_Completed);
                m_asyncSocketUserTokenPool.Push(userToken);
            }
        }

        // Starts the server such that it is listening for
        // incoming connection requests.
        //
        // <param name="localEndPoint">The endpoint which the server will listening
        // for connection requests on</param>
        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(100);
            // post accepts on the listening socket
            StartAccept(null);
            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
            Console.WriteLine("Press any key to terminate the server process....");
            Console.ReadKey();
        }

        // Begins an operation to accept a connection request from the client
        //
        // <param name="acceptEventArg">The context object to use when issuing
        // the accept operation on the server's listening socket</param>
        /// <summary>
        /// 
        /// 
        /// </summary>
        /// <param name="acceptEventArg"></param>
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

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        //
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }


        /// <summary>
        /// 当异步连接完成时调用此方法
        /// </summary>
        /// <param name="e">操作对象</param>
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            // 原子操作,增加一个客户端数量
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server", m_numConnectedSockets);

            // 从接受端重用池获取一个新的SocketAsyncEventArgs对象
            AsyncUserToken userToken = m_asyncSocketUserTokenPool.Pop();
            userToken.Client = e.AcceptSocket;

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

        // This method is called whenever a receive or send operation is completed on a socket
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
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
            // 检查远程主机是否关闭连接
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;

            int offset = userToken.ReceiveEventArgs.Offset;
            int count = userToken.ReceiveEventArgs.BytesTransferred;
            byte[] Buffer = userToken.ReceiveEventArgs.Buffer;

            if (count > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);


                Block block = new Block(Buffer);
                BaseProtocol baseProtocol = new BaseProtocol();
                baseProtocol.Analyze(block);

                while (offset < count)
                {
                    offset++;
                    // 需要处理分包
                    byte[] packet = new byte[baseProtocol.PacketSize];
                    Array.Copy(Buffer, offset, packet, 0, baseProtocol.PacketSize);
                    offset += baseProtocol.PacketSize;
                    // 回调               
                    OnReceive?.Invoke(userToken, packet, offset, baseProtocol.PacketSize);
                    if (baseProtocol.PacketId == 0)
                    {
                        if (baseProtocol.PacketSize > 3)
                        {
                            Handshake handshake = baseProtocol.Resolve<Handshake>();
                            // 开始处理本次收到的数据包
                            SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();
                            using (Block temp = new Block())
                            {
                                temp.WriteInt(0);
                                temp.WriteString("{\"version\":{\"name\":\"1.15.2\",\"protocol\":578},\"players\":{\"max\":100,\"online\":5,\"sample\":[{\"name\":\"thinkofdeath\",\"id\":\"4566e69f-c907-48ee-8d71-d7ba5aa00d20\"}]},\"description\":{\"text\":\"Hello world\"},\"favicon\":\"data:image/png;base64,<data>\"}", true);
                                byte[] buffer = temp.GetBytes();
                                using (MemoryStream memoryStream = new MemoryStream())
                                {
                                    memoryStream.WriteInt(buffer.Length);
                                    memoryStream.Write(buffer);
                                    sendPacket.SetBuffer(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                                    userToken.Client.SendAsync(sendPacket);
                                }
                                Console.Out.WriteLine("New player.");
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
                            Console.Out.WriteLine("New Ping.");
                            userToken.Client.SendAsync(sendPacket);
                        }
                    }
                    baseProtocol.Analyze(block);
                }

                // 准备下次接收数据      
                bool willRaiseEvent = userToken.Client.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
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
                bool willRaiseEvent = token.Client.ReceiveAsync(e);
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

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            // close the socket associated with the client
            try
            {
                token.Client.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Client.Close();
            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);
            // Free the SocketAsyncEventArg so they can be reused by another client
            m_asyncSocketUserTokenPool.Push(token);
            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
    }
}

