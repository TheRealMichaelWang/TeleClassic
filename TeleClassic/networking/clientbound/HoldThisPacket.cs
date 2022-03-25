using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class HoldThisPacket : Packet
    {
        public readonly byte BlockToHold;
        public readonly byte PreventChange;

        public HoldThisPacket(byte blockToHold, byte preventChange) : base(0x14)
        {
            this.BlockToHold = blockToHold;
            this.PreventChange = preventChange;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x14);
            writer.WriteByte(this.BlockToHold);
            writer.WriteByte(this.PreventChange);
        }
    }
}
