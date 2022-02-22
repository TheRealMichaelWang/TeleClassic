using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class LevelFinalizePacket : Packet
    {
        public readonly short XDim;
        public readonly short YDim;
        public readonly short ZDim;

        public LevelFinalizePacket(short xDim, short yDim, short zDim) : base(0x04)
        {
            XDim = xDim;
            YDim = yDim;
            ZDim = zDim;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x04);
            writer.WriteShort(XDim);
            writer.WriteShort(YDim);
            writer.WriteShort(ZDim);
        }
    }
}