using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    class SpawnPlayerPacket : Packet
    {
        public readonly byte PlayerID;
        public readonly string PlayerName;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly byte Yaw;
        public readonly byte Pitch;

        public SpawnPlayerPacket(byte playerID, string playerName,short x, short y, short z, byte yaw, byte pitch) : base(7)
        {
            this.PlayerID = playerID;
            this.PlayerName = playerName;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Yaw = yaw;
            this.Pitch = pitch;
            WriteByte(PlayerID);
            WriteString(playerName);
            WriteShort(x);
            WriteShort(y);
            WriteShort(z);
            WriteByte(yaw);
            WriteByte(pitch);
        }
    }
}
