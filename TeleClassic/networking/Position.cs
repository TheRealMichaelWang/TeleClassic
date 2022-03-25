using TeleClassic.Networking;

namespace TeleClassic.Gameplay
{
    public partial class BlockPosition
    {
        public BlockPosition(MinecraftStreamReader reader)
        {
            X = reader.ReadShort();
            Y = reader.ReadShort();
            Z = reader.ReadShort();
        }

        public virtual void WriteBack(MinecraftStreamWriter writer)
        {
            writer.WriteShort(X);
            writer.WriteShort(Y);
            writer.WriteShort(Z);
        }
    }

    public partial class PlayerPosition
    {
        public PlayerPosition(MinecraftStreamReader reader) : base(reader)
        {
            Heading = reader.ReadByte();
            Pitch = reader.ReadByte();
        }

        public override void WriteBack(MinecraftStreamWriter writer)
        {
            base.WriteBack(writer);
            writer.WriteByte(Heading);
            writer.WriteByte(Pitch);
        }
    }
}