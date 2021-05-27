using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol
{
    public partial class SetBlockPacket : Packet
    {
        public SetBlockPacket(short x, short y, short z, byte blockType) : base(6)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.BlockType = blockType;
            WriteShort(x);
            WriteShort(y);
            WriteShort(z);
            WriteByte(blockType);
        }
    }
}
