namespace TeleClassic.Gameplay
{
    public partial class BlockPosition
    {
        public static bool IsInvalid(BlockPosition blockPosition) => blockPosition.X < 0 || blockPosition.Y < 0 || blockPosition.Z < 0;

        public readonly short X;
        public readonly short Y;
        public readonly short Z;

        public BlockPosition(short x, short y, short z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BlockPosition(PlayerPosition playerPosition) : this((short)(playerPosition.X / 32), (short)((playerPosition.Y - 51) / 32), (short)(playerPosition.Z / 32)) {}

        public BlockPosition(BlockPosition start, short xOffset, short yOffset, short zOffset) : this((short)(start.X + xOffset), (short)(start.Y + yOffset), (short)(start.Z + zOffset)) { }

        public override string ToString() => this.X + ", " + this.Y + ", " + this.Z;
    }

    public sealed partial class PlayerPosition : BlockPosition
    {
        public static class HeadingDirection
        {
            public const byte North = 0;
            public const byte East = 64;
            public const byte South = 128;
            public const byte West = 192;
        }

        public static class PitchDirection
        {
            public const byte Up = 192;
            public const byte Down = 64;
        }

        public readonly byte Heading;
        public readonly byte Pitch;

        public PlayerPosition(short x, short y, short z, byte heading, byte pitch) : base(x, y, z)
        {
            Heading = heading;
            Pitch = pitch;
        }

        public PlayerPosition(BlockPosition blockPosition, byte heading, byte pitch) : this((short)(blockPosition.X * 32), (short)(blockPosition.Y * 32 + 51), (short)(blockPosition.Z * 32), heading, pitch) {}

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(PlayerPosition))
                return false;
            PlayerPosition otherPosition = (PlayerPosition)obj;
            return otherPosition.X == X && otherPosition.Y == Y && otherPosition.Z == Z &&
                otherPosition.Heading == Heading && otherPosition.Pitch == Pitch;
        }
    }
}