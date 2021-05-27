using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic
{
    public static class ServerInformation
    {
        public const string Name = "TeleClassic";
        public const string MessageOfTheDay = "Long live the command line!";
        public const byte Version = 0x07;
        public const byte DefaultUserType = UserType.Standard;
    }

    public static class BlockMode
    {
        public const byte Create = 0x01;
        public const byte Destroy = 0x0;
    }

    public static class Blocks
    {
        public const byte Air = 0;
        public const byte Stone = 1;
        public const byte Grass = 2;
        public const byte Dirt = 3;
        public const byte CobbleStone = 4;
        public const byte Wood = 5;
        public const byte Shrub = 6;
        public const byte Bedrock = 7;
        public const byte Water = 8;
        public const byte Waterstill = 9;
        public const byte Lava = 10;
        public const byte Lavastill = 11;
        public const byte Sand = 12;
        public const byte Gravel = 13;
        public const byte Goldore = 14;
        public const byte Ironore = 15;
        public const byte Coal = 16;
        public const byte Trunk = 17;
        public const byte Leaf = 18;
        public const byte Sponge = 19;
        public const byte Glass = 20;
        public const byte Red = 21;
        public const byte Orange = 22;
        public const byte Yellow = 23;
        public const byte LightGreen = 24;
        public const byte Green = 25;
        public const byte AquaGreen = 26;
        public const byte Cyan = 27;
        public const byte LightBlue = 28;
        public const byte Blue = 29;
        public const byte Purple = 30;
        public const byte LightPurple = 31;
        public const byte Pink = 32;
        public const byte DarkPink = 33;
        public const byte DarkGrey = 34;
        public const byte LightGrey = 35;
        public const byte White = 36;
        public const byte YellowFlower = 37;
        public const byte RedFlower = 38;
        public const byte Mushroom = 39;
        public const byte RedMushroom = 40;
        public const byte GoldSolid = 41;
        public const byte IronSolid = 42;
        public const byte StaircaseFull = 43;
        public const byte StaircaseStep = 44;
        public const byte Brick = 45;
        public const byte TNT = 46;
        public const byte BookCase = 47;
        public const byte MossyCobble = 48;
        public const byte Obsidian = 49;

        public const byte Error = 255;
    }

    public static class UserType
    {
        public const byte Operator = 0x64;
        public const byte Standard = 0x0;
    }

    public static class Orientation
    {
        public const byte North = 0;
        public const byte East = 64;
        public const byte South = 128;
        public const byte West = 192;
    }

    public static class ColorCode
    {
        public const string Black = "&0";
        public const string DarkBlue = "&1";
        public const string DarkGreen = "&2";
        public const string DarkTeal = "&3";
        public const string DarkRed = "&4";
        public const string Purple = "&5";
        public const string Gold = "&6";
        public const string Gray = "&7";
        public const string DarkGray = "&8";
        public const string Blue = "&9";
        public const string BrightGreen = "&a";
        public const string Teal = "&b";
        public const string Red = "&c";
        public const string Pink = "&d";
        public const string Yellow = "&e";
        public const string White = "&f";
    }
}
