using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class RemoveSelectionPacket : Packet
    {
        public readonly byte SelectionID;

        public RemoveSelectionPacket(byte selectionID) : base(0x1b)
        {
            this.SelectionID = selectionID;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x1b);
            writer.WriteByte(this.SelectionID);
        }
    }
}
