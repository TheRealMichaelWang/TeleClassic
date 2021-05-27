using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class ServerIdentificationPacket : Packet
    {
        public readonly byte ProtocolVersion;
        public readonly string ServerName;
        public readonly string MessageOfTheDay;
        public readonly byte UserType;

        public ServerIdentificationPacket(byte protocolVersion, string serverName, string messageOfTheDay, byte userType):base(0)
        {
            this.ProtocolVersion = protocolVersion;
            this.ServerName = serverName;
            this.MessageOfTheDay = messageOfTheDay;
            this.UserType = userType;
            WriteByte(ProtocolVersion);
            WriteString(serverName);
            WriteString(messageOfTheDay);
            WriteByte(userType);
        }
    }
}
