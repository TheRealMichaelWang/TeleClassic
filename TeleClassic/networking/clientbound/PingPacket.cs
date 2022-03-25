using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class PingPacket : Packet
    {
        public PingPacket() : base(0x01) { }

        public override void Send(NetworkStream stream) => stream.WriteByte(0x01);
    }
}