using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol
{
    public partial class PositionAndOrientationPacket : Packet
    {
        public PositionAndOrientationPacket(sbyte id, short x, short y, short z, byte yaw, byte pitch):base(8)
        {
            this.PlayerID = Convert.ToByte(id);
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Yaw = yaw;
            this.Pitch = pitch;
            WriteByte(PlayerID);
            WriteShort(x);
            WriteShort(y);
            WriteShort(z);
            WriteByte(yaw);
            WriteByte(pitch);
        }
    }
}
