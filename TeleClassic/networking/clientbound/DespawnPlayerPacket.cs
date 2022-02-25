using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class DespawnPlayerPacket : Packet
    {
        public readonly sbyte PlayerID;

        public DespawnPlayerPacket(sbyte playerID) : base(0xc)
        {
            PlayerID = playerID;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0xc);
            writer.WriteSByte(PlayerID);
        }
    }
}