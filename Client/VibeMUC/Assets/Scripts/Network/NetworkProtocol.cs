using System;

namespace VibeMUC.Network
{
    public static class NetworkConstants
    {
        public const int DefaultPort = 5000;
        public const int MaxMessageSize = 1024 * 1024; // 1MB max message size
    }

    public enum MessageType : byte
    {
        RequestMap = 1,
        MapData = 2,
        PlayerMove = 3,
        PlayerJoin = 4,
        PlayerLeave = 5,
        Error = 255
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public byte[] Payload { get; set; }
    }
} 