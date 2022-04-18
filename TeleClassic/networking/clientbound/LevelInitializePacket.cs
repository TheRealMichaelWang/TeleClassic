using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class LevelInitializePacket : Packet
    {
        public LevelInitializePacket() : base(0x02) { }

        public override void Send(NetworkStream stream) => stream.WriteByte(0x02);
    }
}