using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.gameplay
{
    public static class Physics
    {
        public static bool RequiresGround(byte block)
        {
            return block == Blocks.RedFlower || block == Blocks.YellowFlower || block == Blocks.Mushroom || block == Blocks.RedMushroom;
        }

        public static bool CanPass(byte block)
        {
            return block == Blocks.Air || IsLiquid(block);
        }

        public static bool IsLiquid(byte block)
        {
            return block == Blocks.Lava || block == Blocks.Water;
        }

        public static bool IsLoose(byte block)
        {
            return block == Blocks.Sand || block == Blocks.Gravel;
        }

        public static void Update(object[] args) => Update((MultiplayerWorld)args[0], (short)args[1], (short)args[2], (short)args[3]);

        public static void Update(MultiplayerWorld world, short x, short y, short z)
        {
            if(world[x,y,z] == Blocks.Error)
            {
                return; //out of bounds;
            }
            else if(world[x, y, z] == Blocks.Air)
            {
                if (IsLoose(world[x, (short)(y + 1), z]))
                    Gameplay.ExecuteTask(Update, world, x, (short)(y + 1), z);
                else if (RequiresGround(world[x, (short)(y + 1), z]))
                    world[x, (short)(y + 1), z] = Blocks.Air;
                else
                    for (sbyte dx = -1; dx <= 1; dx++)
                        for (sbyte dy = 0; dy <= 1; dy++)
                            for (sbyte dz = -1; dz <= 1; dz++)
                                if (!(dx == 0 && dy == 0 && dz == 0) && (dx == 0 || dy == 0 || dz == 0))
                                {
                                    if (IsLiquid(world[(short)(x + dx), (short)(y + dy), (short)(z + dz)]))
                                        Gameplay.ExecuteTask(Update, world, (short)(x + dx), (short)(y + dy), (short)(z + dz));
                                }
            }
            else if(IsLoose(world[x, y, z]))
            {
                if(CanPass(world[x,(short)(y-1),z]))
                {
                    if (IsLiquid(world[x, (short)(y - 1), z]) && !(IsLiquid(world[(short)(x + 1), y, z]) || IsLiquid(world[(short)(x - 1), y, z]) || IsLiquid(world[x, y, (short)(z + 1)]) || IsLiquid(world[x, y, (short)(z - 1)])))
                    {
                        world[x, (short)(y - 1), z] = world[x, y, z];
                        world[x, y, z] = Blocks.Air;
                    }
                    else
                    {
                        SwapBlocks(world, x, y, z, x, (short)(y - 1), z);
                    }
                    Gameplay.ExecuteTask(Update, world, x, (short)(y - 1), z);
                    if(IsLoose(world[x, (short)(y + 1), z]))
                        Gameplay.ExecuteTask(Update, world, x, (short)(y + 1), z);
                }
            }
            else if(IsLiquid(world[x, y, z]))
            {
                byte fillwith = world[x, y, z];
                world[x, y, z] = Blocks.Air;
                Gameplay.ExecuteTask(FillLiquid, world, x, y, z, fillwith);
            }
        }

        private static void FillLiquid(object[] args) => FillLiquid((MultiplayerWorld)args[0], (short)args[1], (short)args[2], (short)args[3], (byte)args[4]);

        private static void FillLiquid(MultiplayerWorld world, short x, short y, short z, byte liquid)
        {
            if(world[x,y,z] != Blocks.Air)
            {
                return;
            }
            world[x, y, z] = liquid;
            Gameplay.ExecuteTask(FillLiquid, world, (short)(x + 1), y, z, liquid);
            Gameplay.ExecuteTask(FillLiquid, world, (short)(x - 1), y, z, liquid);
            Gameplay.ExecuteTask(FillLiquid, world, x, y, (short)(z + 1), liquid);
            Gameplay.ExecuteTask(FillLiquid, world, x, y, (short)(z - 1), liquid);
            Gameplay.ExecuteTask(FillLiquid, world, x, (short)(y - 1), z, liquid);
        }

        private static void SwapBlocks(MultiplayerWorld world, short x1, short y1, short z1, short x2, short y2, short z2)
        {
            byte temp = world[x1, y1, z1];
            world[x1, y1, z1] = world[x2, y2, z2];
            world[x2, y2, z2] = temp;
        }
    }

    public partial class MultiplayerWorld
    { 
        
    }
}
