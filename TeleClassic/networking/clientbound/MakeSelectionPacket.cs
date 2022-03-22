using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class MakeSelectionPacket : Packet
    {
        public readonly byte SelectionID;
        public readonly string Label;
        
        public readonly BlockPosition Start;
        public readonly BlockPosition End;

        public readonly short Red;
        public readonly short Green;
        public readonly short Blue;

        public readonly short Opacity;

        public MakeSelectionPacket(byte selectionId, string label, BlockPosition start, BlockPosition end, short red, short green, short blue, short opacity) : base(0x1a)
        {
            this.SelectionID = selectionId;
            this.Label = label;
            this.Start = start;
            this.End = end;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Opacity = opacity;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x1a);
            writer.WriteByte(this.SelectionID);
            writer.WriteString(this.Label);
            this.Start.WriteBack(writer);
            this.End.WriteBack(writer);
            writer.WriteShort(this.Red);
            writer.WriteShort(this.Green);
            writer.WriteShort(this.Blue);
            writer.WriteShort(this.Opacity);
        }
    }
}
