using System;
using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking.Serverbound
{
    public sealed class SetBlockPacket : Packet
    {
        public readonly BlockPosition Position;

        public readonly byte Mode;
        public readonly byte BlockType;

        public SetBlockPacket(NetworkStream stream) : base(0x05)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            Position = new BlockPosition(reader);
            Mode = reader.ReadByte();
            BlockType = reader.ReadByte();
        }

        public override void Send(NetworkStream stream) => throw new InvalidOperationException();
    }
}