using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.Networking.Clientbound
{
    public class EnvSetAppearancePacket1 : Packet
    {
        public readonly string TexturePackURL;
        public readonly byte SideBlock;
        public readonly byte EdgeBlock;
        public readonly short SideLevel;

        public EnvSetAppearancePacket1(string texturePackUrl, byte sideBlock, byte edgeBlock, short sideLevel) : base(0x1e)
        {
            this.TexturePackURL = texturePackUrl;
            this.SideBlock = sideBlock;
            this.EdgeBlock = edgeBlock;
            this.SideLevel = sideLevel;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x1e);
            writer.WriteString(this.TexturePackURL);
            writer.WriteByte(this.SideBlock);
            writer.WriteByte(this.EdgeBlock);
            writer.WriteShort(this.SideLevel);
        }
    }

    public sealed class EnvSetAppearancePacket2 : EnvSetAppearancePacket1
    {
        public readonly short CloudLevel;
        public readonly short MaximumViewDistance;

        public EnvSetAppearancePacket2(string texturePackUrl, byte sideBlock, byte edgeBlock, short sideLevel, short cloudLevel, short maximumViewDistance) : base(texturePackUrl, sideBlock, edgeBlock, sideLevel)
        {
            this.CloudLevel = cloudLevel;
            this.MaximumViewDistance = maximumViewDistance;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x1e);
            writer.WriteString(this.TexturePackURL);
            writer.WriteByte(this.SideBlock);
            writer.WriteByte(this.EdgeBlock);
            writer.WriteShort(this.SideLevel);
            writer.WriteShort(this.CloudLevel);
            writer.WriteShort(this.SideLevel);
        }
    }
}
