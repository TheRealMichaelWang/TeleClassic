using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class SpawnPlayerPacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly string PlayerName;
        public readonly PlayerPosition Position;

        public SpawnPlayerPacket(sbyte playerID, string playerName, PlayerPosition position) : base(0x07)
        {
            PlayerID = playerID;
            PlayerName = playerName;
            Position = position;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x07);
            writer.WriteSByte(PlayerID);
            writer.WriteString(PlayerName);
            Position.WriteBack(writer);
        }
    }
}