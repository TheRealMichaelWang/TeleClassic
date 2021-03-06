using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.Gameplay
{
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
    }

    public static class ExtendedBlocks
    {
        private static byte[] FallBackBlocks =
        {
            Blocks.StaircaseStep,
            Blocks.Mushroom,
            Blocks.Sand,
            Blocks.Air,
            Blocks.Lava,
            Blocks.Pink,
            Blocks.Green,
            Blocks.Dirt,
            Blocks.Blue,
            Blocks.Cyan,
            Blocks.Glass,
            Blocks.Ironore,
            Blocks.Stone
        };

        public static byte MaxSupportLevel = 1;

        public const byte CobbleStoneSlab = 50;
        public const byte Rope = 51;
        public const byte Sandstone = 52;
        public const byte Snow = 53;
        public const byte Fire = 54;
        public const byte LightPink = 55;
        public const byte ForestGreen = 56;
        public const byte BrownWool = 58;
        public const byte DeepBlue = 58;
        public const byte Turquoise = 59;
        public const byte Ice = 60;
        public const byte CeramicTile = 61;
        public const byte Magma = 62;
        public const byte Pillar = 63;
        public const byte Crate = 64;
        public const byte StoneBrick = 65;

        public static bool IsExtendedBlock(byte block) => block >= 50;

        public static byte GetExtendedBlockFallback(byte block)
        {
            if (!IsExtendedBlock(block))
                throw new InvalidOperationException();
            return FallBackBlocks[block - 50];
        }

        public static byte GetExtendedBlockSupportLevel(byte block)
        {
            if (!IsExtendedBlock(block))
                throw new InvalidOperationException();
            if (block >= 50 && block <= 65)
                return 1;
            else
                throw new InvalidOperationException();
        }
    }
}
