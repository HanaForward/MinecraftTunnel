using MinecraftTunnel.Protocol.ClientBound;
using MinecraftTunnel.Protocol.ServerBound;
using System;
using System.Net;

namespace MinecraftTunnel
{
    public class MinecraftTunnel
    {

        public static void Main()
        {

            /*
            StateContext stateContext = new StateContext(5, 500);
            stateContext.Init();
            IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, 25565);
            stateContext.Start(serverIP);
            */


            Tunnel tunnel = new Tunnel("172.65.234.205", 25565);

            Handshake handshake = new Handshake();
            handshake.ProtocolVersion = 578;
            handshake.ServerPort = 25565;
            handshake.ServerAddress = "mc.hypixel.net";
            handshake.NextState = NextState.status;
            tunnel.Login(handshake);


            Pong pong = new Pong();
            pong.Payload = 999999;
            tunnel.Ping(pong);


            Console.ReadKey();
        }
    }
}
