using System;

namespace VibeMUC.Network
{
    public enum MessageType : byte
    {
        RequestMap = 1,
        MapData = 2,
        PlayerMove = 3,
        PlayerJoin = 4,
        PlayerLeave = 5,
        Error = 255
    }

    [Serializable]
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public required byte[] Payload { get; set; }
    }

    public static class NetworkConstants
    {
        public const int DefaultPort = 5000;
        public const int MaxMessageSize = 1024 * 1024; // 1MB max message size
        public const int HeaderSize = sizeof(MessageType) + sizeof(int); // MessageType + PayloadSize
    }
} 