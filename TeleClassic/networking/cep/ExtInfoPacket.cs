using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.Networking.CEP
{
    public sealed class ExtInfoPacket : Packet
    {
        public readonly string AppName;
        public readonly short ExtensionCount;

        public ExtInfoPacket(string appName, short extensionCount) : base(0x10)
        {
            this.AppName = appName;
            this.ExtensionCount = extensionCount;
        }

        public ExtInfoPacket(NetworkStream stream) : base(0x10)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            this.AppName = reader.ReadString();
            this.ExtensionCount = reader.ReadShort();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x10);
            writer.WriteString(this.AppName);
            writer.WriteShort(this.ExtensionCount);
        }
    }
}
