using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.serverbound
{
    public class MessagePacket : Packet
    {
        public readonly string Message;

        public MessagePacket(byte[] data):base(data)
        {
            ReadByte(); //unused
            Message = ReadString().Trim();
        }
    }
}
