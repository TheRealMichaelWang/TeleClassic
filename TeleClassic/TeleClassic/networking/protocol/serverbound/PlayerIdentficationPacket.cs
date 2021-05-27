using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.serverbound
{
    public class PlayerIdentficationPacket : Packet
    {
        public readonly byte ProtocolVersion;
        public readonly string Username;
        public readonly string VerficiationKey;

        public PlayerIdentficationPacket(byte[] data) : base(data)
        {
            ProtocolVersion = ReadByte();
            Username = ReadString().Trim();
            VerficiationKey = ReadString().Trim();
            ReadByte(); //read unused byte
        }
    }
}
