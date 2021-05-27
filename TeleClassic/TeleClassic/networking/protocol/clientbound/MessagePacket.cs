using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    class MessagePacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly string Message;

        public MessagePacket(sbyte playerID, string message) : base(13)
        {
            this.PlayerID = playerID;
            this.Message = message;
            WriteSignedByte(playerID);
            WriteString(message);
        }
    }
}
