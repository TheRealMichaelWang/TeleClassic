using System.Net.Sockets;

namespace TeleClassic.Networking
{
    public sealed class IdentificationPacket : Packet
    {
        public readonly string Name;
        public readonly string Key;
        public readonly byte ProtocolVersion;
        public readonly byte Permissions;

        public IdentificationPacket(string name, string key, byte protocolVersion, byte permissions) : base(0)
        {
            Name = name;
            Key = key;
            ProtocolVersion = protocolVersion;
            Permissions = permissions;
        }

        public IdentificationPacket(NetworkStream stream) : base(0)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            ProtocolVersion = reader.ReadByte();
            Name = reader.ReadString();
            Key = reader.ReadString();
            Permissions = reader.ReadByte();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0);
            writer.WriteByte(ProtocolVersion);
            writer.WriteString(Name);
            writer.WriteString(Key);
            writer.WriteByte(Permissions);
        }
    }
}