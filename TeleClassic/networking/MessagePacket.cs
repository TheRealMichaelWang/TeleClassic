using System.Net.Sockets;

namespace TeleClassic.Networking
{
    public sealed class MessagePacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly string Message;

        public MessagePacket(sbyte playerID, string message) : base(0x0d)
        {
            PlayerID = playerID;
            Message = message;
        }

        public MessagePacket(NetworkStream stream) : base(0x0d)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            PlayerID = reader.ReadSByte();
            Message = reader.ReadString();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x0d);
            writer.WriteSByte(PlayerID);
            writer.WriteString(Message);
        }
    }
}