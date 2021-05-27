using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class PingPacket:Packet
    {
        public PingPacket():base(1)
        {

        }
    }
}
