using MinecraftTunnel.Extensions;
using MinecraftTunnel.Protocol;
using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MinecraftTunnel
{
    public class Tunnel
    {
        private Socket socket;
        Thread thread;
        private AsyncUserToken asyncUserToken;

        public Tunnel(string IP, int Port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(IPAddress.Parse(IP), Port);
            }
            catch
            {
                Console.WriteLine("连接服务器失败，请按回车键退出！");
                return;
            }


            thread = new Thread(StartReceive);
            thread.IsBackground = true;
            thread.Start(socket);

            Thread.Sleep(200);
        }

        internal void Ping(Pong pong)
        {
            
            byte[] buffer;
            using (Block block = new Block())
            {
                block.WriteInt(1);
                block.WriteLong(pong.Payload);
                buffer = block.GetBytes();
            }
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.WriteInt(buffer.Length);
            memoryStream.Write(buffer);
            socket.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Position - 1, SocketFlags.None);
        }

        private void StartReceive(object obj)
        {
            Socket receiveSocket = obj as Socket;
            while (true)
            {
                byte[] buf = new byte[ushort.MaxValue];
                int Size = receiveSocket.Receive(buf);
                Console.WriteLine("接收服务器消息：{0}", Encoding.ASCII.GetString(buf, 0, Size));
            }
        }

        public void Bind(AsyncUserToken asyncUserToken)
        {
            this.asyncUserToken = asyncUserToken;
        }

        public void Login(Handshake handshake)
        {
            byte[] buffer;
            using (Block block = new Block())
            {
                block.WriteInt(0);
                block.WriteInt(handshake.ProtocolVersion);
                block.WriteString("mc.hypixel.net", true);
                block.WriteUShort(handshake.ServerPort);
                block.WriteInt((int)handshake.NextState);
                buffer = block.GetBytes();
            }
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.WriteInt(buffer.Length);
            memoryStream.Write(buffer);
            socket.Send(memoryStream.GetBuffer(), 0, (int)memoryStream.Position - 1, SocketFlags.None);
        }


        public void IO_Completed(object arg1, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    //ProcessSend(e);
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
            byte[] receiveBuffer = new byte[e.BytesTransferred];
            Array.Copy(userToken.ReceiveEventArgs.Buffer, e.Offset, receiveBuffer, 0, e.BytesTransferred);
            socket.Send(receiveBuffer);
        }
    }
}