using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol.clientbound
{
    public class UpdateUserType : Packet
    {
        public readonly byte UserType;

        public UpdateUserType(byte userType) : base(15)
        {
            this.UserType = userType;
            WriteByte(userType);
        }
    }
}
