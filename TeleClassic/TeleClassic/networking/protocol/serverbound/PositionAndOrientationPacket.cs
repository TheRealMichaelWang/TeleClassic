using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol
{
    public partial class PositionAndOrientationPacket : Packet
    {
        public readonly byte PlayerID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly byte Yaw;
        public readonly byte Pitch;

        public PositionAndOrientationPacket(byte[] data):base(data)
        {
            PlayerID = ReadByte();
            X = ReadShort();
            Y = ReadShort();
            Z = ReadShort();
            Yaw = ReadByte();
            Pitch = ReadByte();
        }
    }
}
