using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class LevelInitializePacket : Packet
    {
        public LevelInitializePacket():base(2)
        {

        }
    }
}
