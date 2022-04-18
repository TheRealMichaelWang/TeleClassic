using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class DisconnectPlayerPacket : Packet
    {
        public readonly string Reason;

        public DisconnectPlayerPacket(string reason) : base(0x0e)
        {
            Reason = reason;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x0e);
            writer.WriteString(Reason);
        }
    }
}