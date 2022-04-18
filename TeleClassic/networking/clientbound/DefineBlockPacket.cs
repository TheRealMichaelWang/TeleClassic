using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class DefineBlockPacket : Packet
    {
        public readonly World.CustomBlockDefinition CustomBlockDefinition;

        public DefineBlockPacket(World.CustomBlockDefinition customBlockDefinition):base(0x23)
        {
            this.CustomBlockDefinition = customBlockDefinition;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x23);
            writer.WriteByte(CustomBlockDefinition.BlockID);
            writer.WriteString(CustomBlockDefinition.Name);
            writer.WriteByte((byte)CustomBlockDefinition.Solidity);
            //writer.WriteByte((byte)(Math.Log(CustomBlockDefinition.MovementSpeed, 2) * 64 + 128));
            writer.WriteByte((byte)Math.Pow(2, (CustomBlockDefinition.MovementSpeed - 128) / 64));
            writer.WriteByte(CustomBlockDefinition.TextureInfo.TopTextureID);
            writer.WriteByte(CustomBlockDefinition.TextureInfo.SideTextureID);
            writer.WriteByte(CustomBlockDefinition.TextureInfo.BottomTextureID);
            writer.WriteByte(CustomBlockDefinition.TransmitsLight ? (byte)1 : (byte)0);
            writer.WriteByte((byte)CustomBlockDefinition.Sound);
            writer.WriteByte(CustomBlockDefinition.FullBright ? (byte)1 : (byte)0);
            writer.WriteByte(CustomBlockDefinition.Shape);
            writer.WriteByte((byte)CustomBlockDefinition.DrawMode);
            writer.WriteByte(CustomBlockDefinition.FogDensity);
            writer.WriteByte(CustomBlockDefinition.FogColor.R);
            writer.WriteByte(CustomBlockDefinition.FogColor.G);
            writer.WriteByte(CustomBlockDefinition.FogColor.B);
        }
    }

    public sealed class RemoveBlockDefinitionPacket : Packet
    {
        public readonly byte BlockID;

        public RemoveBlockDefinitionPacket(byte blockID) : base(0x24)
        {
            this.BlockID = blockID;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x24);
            writer.WriteByte(this.BlockID);
        }
    }
}
