namespace MinecraftTunnel.Protocol
{
    public static class Packet
    {
        public static ServerPacket ServerPacket;
        public static ClientPakcet ClientPakcet;
    }

    public enum ServerPacket
    {
        LoginStart = 0,
        EncryptionResponse = 1,
        LoginPluginResponse = 2,
        SetCompression = 3
    }

    public enum ClientPakcet
    {
        Disconnect = 0,
        EncryptionRequest = 1,
        LoginSuccess = 2,
        SetCompression = 3,
        LoginPluginRequest = 4,
    }
}
