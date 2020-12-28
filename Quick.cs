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
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations

        private AsyncUserTokenPool m_asyncSocketUserTokenPool;

        int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server
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

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
                m_numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the
            //ReadEventArg object user token

            AsyncUserToken userToken = m_asyncSocketUserTokenPool.Pop();
            userToken.Client = e.AcceptSocket;
            //SocketAsyncEventArgs readEventArgs = m_asyncSocketUserTokenPool.Pop();
            //((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;
            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(userToken.ReceiveEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(userToken.ReceiveEventArgs);
            }

            // Accept the next connection request
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
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        // This method is invoked when an asynchronous receive operation completes.
        // If the remote host closed the connection, then the socket is closed.
        // If data was received then the data is echoed back to the client.
        //
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
            if (userToken.ReceiveEventArgs.BytesTransferred > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                int offset = userToken.ReceiveEventArgs.Offset;
                int count = userToken.ReceiveEventArgs.BytesTransferred;

                if (count > 0)
                {
                    Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                    Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

 
                    SocketAsyncEventArgs sendPacket = new SocketAsyncEventArgs();

                    byte[] receiveBuffer = new byte[e.BytesTransferred];
                    Array.Copy(userToken.ReceiveEventArgs.Buffer, e.Offset, receiveBuffer, 0, e.BytesTransferred);

                    Block receive = new Block(receiveBuffer);
                    int PacketSize = receive.readVarInt();
                    int PacketId = receive.readVarInt();

                    if (PacketId == 0)
                    {

                        Handshake handshake = new Handshake();
                        handshake.ProtocolVersion = receive.readVarInt();
                        int ServerAddressLength = receive.readVarInt();
                        handshake.ServerAddress = receive.readString(ServerAddressLength);
                        handshake.ServerPort = receive.readShort();
                        handshake.NextState = (NextState)receive.readVarInt();

                        if (handshake.NextState == NextState.status)
                        {
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
                        else if (handshake.NextState == NextState.login)
                        {
                           
                            userToken.Tunnel();
                            userToken.tunnel.Login(handshake);
                            return;
                        }
                    }
                    else if (PacketId == 1)
                    {
                        Pong pong = new Pong();
                        pong.Payload = receive.readLong();
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
                }
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

