using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking
{
    public sealed class PositionAndOrientationPacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly PlayerPosition Position;

        public PositionAndOrientationPacket(sbyte playerID, PlayerPosition position) : base(0x08)
        {
            PlayerID = playerID;
            Position = position;
        }

        public PositionAndOrientationPacket(NetworkStream stream) : base(0x08)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            PlayerID = reader.ReadSByte();
            Position = new PlayerPosition(reader);
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x08);
            writer.WriteSByte(PlayerID);
            Position.WriteBack(writer);
        }
    }
}