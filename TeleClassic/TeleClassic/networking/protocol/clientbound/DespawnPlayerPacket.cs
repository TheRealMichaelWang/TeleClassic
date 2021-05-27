using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class DespawnPlayerPacket:Packet
    {
        public readonly sbyte PlayerID;

        public DespawnPlayerPacket(sbyte playerID) : base(12)
        {
            this.PlayerID = playerID;
            WriteSignedByte(playerID);
        }
    }
}
