using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ServerBound;
using socket.core.Client;
using System;
using System.Net.Sockets;

namespace MinecraftTunnel
{
    public class Tunnel
    {
        private readonly TcpPushClient client;
        private StateContext stateContext;
        private AsyncUserToken userToken;
        public Tunnel(string IP, int Port)
        {
            client = new TcpPushClient(ushort.MaxValue);
            client.OnReceive += Client_OnReceive;
            client.Connect(IP, Port);
        }

        private void Client_OnReceive(byte[] obj)
        {
            userToken.ServerSocket.Send(obj);
#if DEBUG
            Console.WriteLine("Client_OnReceive : " + obj.Length);
#endif
        }

        public void Bind(StateContext stateContext, AsyncUserToken asyncUserToken)
        {
            this.stateContext = stateContext;
            this.userToken = asyncUserToken;
        }

        public void Login(string Name, int ProtocolVersion)
        {
            BaseProtocol baseProtocol = new BaseProtocol();
            Handshake handshake = new Handshake();
            handshake.ProtocolVersion = ProtocolVersion;

            handshake.ServerAddress = Program.QueryConfig.ServerAddress;
            handshake.ServerPort = Program.NatConfig.Port;

            handshake.NextState = NextState.login;

            byte[] buffer = baseProtocol.Pack(handshake);
            client.Send(buffer, 0, buffer.Length);

            Login login = new Login();
            login.Name = Name;
            buffer = baseProtocol.Pack(login);
            client.Send(buffer, 0, buffer.Length);
            Console.WriteLine($"Login完毕");
        }

        public void IO_Completed(object arg1, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken userToken = (AsyncUserToken)e.UserToken;
            int offset = userToken.ReceiveEventArgs.Offset;
            int count = userToken.ReceiveEventArgs.BytesTransferred;
            byte[] Buffer = userToken.ReceiveEventArgs.Buffer;
            if (count > 0 && userToken.ReceiveEventArgs.SocketError == SocketError.Success)
            {
                client.Send(Buffer, offset, count);
                // 准备下次接收数据      
                bool willRaiseEvent = userToken.ServerSocket.ReceiveAsync(userToken.ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                    ProcessReceive(userToken.ReceiveEventArgs);
            }
            else
            {
                stateContext.CloseClientSocket(e);
                Close();
            }
        }

        public void Close()
        {
            userToken = null;
            client.Close();
        }
    }
}