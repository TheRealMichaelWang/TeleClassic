using System.Net.Sockets;

namespace TeleClassic.Networking
{
    public sealed class CustomBlockSupportLevelPacket : Packet
    {
        public readonly byte SupportLevel;

        public CustomBlockSupportLevelPacket(byte supportLevel) : base(0x13)
        {
            this.SupportLevel = supportLevel;
        }

        public CustomBlockSupportLevelPacket(NetworkStream stream) : base(0x13)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            this.SupportLevel = reader.ReadByte();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x13);
            writer.WriteByte(this.SupportLevel);
        }
    }
}
