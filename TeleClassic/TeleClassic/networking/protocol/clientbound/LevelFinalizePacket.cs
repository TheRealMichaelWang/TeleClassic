using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    class LevelFinalizePacket : Packet
    {
        public readonly short X;
        public readonly short Y;
        public readonly short Z;

        public LevelFinalizePacket(short x, short y, short z):base(4)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            WriteShort(X);
            WriteShort(Y);
            WriteShort(Z);
        }
    }
}
