using System.Net.Sockets;

namespace TeleClassic.Networking.CEP
{
    public sealed class ExtEntryPacket : Packet
    {
        public readonly string ExtName;
        public readonly int Version;

        public ExtEntryPacket(string extName, int version) : base(0x11)
        {
            this.ExtName = extName;
            this.Version = version;
        }

        public ExtEntryPacket(NetworkStream stream) : base(0x11)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            this.ExtName = reader.ReadString();
            this.Version = reader.ReadInt();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x11);
            writer.WriteString(this.ExtName);
            writer.WriteInt(this.Version);
        }
    }
}
