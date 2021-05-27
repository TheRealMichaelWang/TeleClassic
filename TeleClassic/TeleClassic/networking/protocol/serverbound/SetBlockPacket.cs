using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol
{
    public partial class SetBlockPacket : Packet
    {
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly byte Mode;
        public readonly byte BlockType;

        public SetBlockPacket(byte[] data) : base(data)
        {
            X = ReadShort();
            Y = ReadShort();
            Z = ReadShort();
            Mode = ReadByte();
            BlockType = ReadByte();
        }
    }
}
