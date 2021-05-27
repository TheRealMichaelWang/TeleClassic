using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.gameplay
{
    public class Location
    {
        public readonly string Identfier;
        public Position Position;

        public Location(string identfier, Position position)
        {
            this.Identfier = identfier;
            this.Position = position;
        }
    }

    public class Position : ICloneable
    {
        public static readonly Position Zero = new Position(0, 0, 0, 0, 0);

        public short X;
        public short Y;
        public short Z;
        public byte Yaw;
        public byte Pitch;

        public Position(short x, short y, short z, byte yaw, byte pitch)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Yaw = yaw;
            this.Pitch = pitch;
        }

        public object Clone()
        {
            return new Position(X, Y, Z, Yaw, Pitch);
        }
        
        public override bool Equals(object obj)
        {
            if(!(obj is Position))
            {
                return false;
            }
            Position toCompare = obj as Position;
            return toCompare.X == X && toCompare.Y == Y && toCompare.Z == Z;
        }
    }
}
