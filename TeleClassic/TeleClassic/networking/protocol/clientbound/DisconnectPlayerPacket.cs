using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class DisconnectPlayerPacket:Packet
    {
        public readonly string Reason;

        public DisconnectPlayerPacket(string reason):base(14)
        {
            this.Reason = reason;
            WriteString(reason);
        }
    }
}
