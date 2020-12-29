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
        private AsyncUserToken asyncUserToken;
        public Tunnel(string IP, int Port)
        {
            client = new TcpPushClient(ushort.MaxValue);
            client.OnReceive += Client_OnReceive;
            client.Connect(IP, Port);
        }

        private void Client_OnReceive(byte[] obj)
        {
            asyncUserToken.Client.Send(obj);
#if DEBUG
            Console.WriteLine("Client_OnReceive : " + obj.Length);
#endif
        }
        public void Bind(AsyncUserToken asyncUserToken)
        {
            this.asyncUserToken = asyncUserToken;
        }

        public void Login(string Name)
        {
            BaseProtocol baseProtocol = new BaseProtocol();
            Handshake handshake = new Handshake();
            handshake.ProtocolVersion = 578;
            handshake.ServerAddress = "mc.hypixel.net";
            handshake.ServerPort = 25565;
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
            client.Send(Buffer, offset, count);
        }

        public void Clost()
        {
            asyncUserToken = null;
            client.Close();
        }
    }
}