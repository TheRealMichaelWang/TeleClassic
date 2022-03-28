using System.Net.Sockets;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class SetBlockPacket : Packet
    {
        public readonly BlockPosition Position;
        public readonly byte BlockType;

        public SetBlockPacket(BlockPosition position, byte blockType) : base(0x06)
        {
            Position = position;
            BlockType = blockType;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x06);
            Position.WriteBack(writer);
            writer.WriteByte(BlockType);
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        public void SetBlock(BlockPosition blockPosition, byte blockType)
        {
            if (SupportsBlock(blockType))
                this.SendPacket(new SetBlockPacket(blockPosition, blockType));
            else
                this.SendPacket(new SetBlockPacket(blockPosition, ExtendedBlocks.GetExtendedBlockFallback(blockType)));
        }
    }
}