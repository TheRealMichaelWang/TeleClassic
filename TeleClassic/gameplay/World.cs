using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Gameplay
{
    public class World
    {
        public string Name { get; protected set; }
        public PlayerPosition SpawnPoint;

        protected NBT nBT;

        public byte[] Blocks;

        public readonly short XDim;
        public readonly short YDim;
        public readonly short ZDim;

        public readonly byte FormatVersion; //who gives a fuck?

        private int IndexFromPosition(BlockPosition position) => (position.Y * ZDim + position.Z) * XDim + position.X;
        protected bool edited;

        protected void FillBlocks(short x, short y, short z, short xDim, short yDim, short zDim, byte blockType)
        {
            for(short cx = x; cx < x + xDim; cx++)
                for(short cy = y; cy < y + yDim; cy++)
                    for(short cz = z; cz < z + zDim; cz++)
                        this.Blocks[(cy * this.ZDim + cz) * this.XDim + cx] = blockType;
        }

        public World(string fileName)
        {
            Name = fileName;
            nBT = new NBT(fileName);

            try
            {
                //read a world 
                FormatVersion = (byte)nBT.FindObject("FormatVersion");

                XDim = (short)nBT.FindObject("X");
                YDim = (short)nBT.FindObject("Y");
                ZDim = (short)nBT.FindObject("Z");

                SpawnPoint = new PlayerPosition(new BlockPosition((short)nBT.FindObject("Spawn.X"), (short)nBT.FindObject("Spawn.Y"), (short)nBT.FindObject("Spawn.Z")), PlayerPosition.HeadingDirection.North, PlayerPosition.PitchDirection.Up);

                Blocks = (byte[])nBT.FindObject("BlockArray");

                Logger.Log("Info", "Sucesfully loaded world.", fileName);
                edited = false;
            }
            catch (KeyNotFoundException)
            {
                Logger.Log("Error", "Unable to load world.", fileName);
                Logger.Log("Info", "Overriding/generating new world.", fileName);

                this.FormatVersion = 1;
                XDim = 64;
                YDim = 32;
                ZDim = 64;

                SpawnPoint = new PlayerPosition(new BlockPosition(32, 17, 32), PlayerPosition.HeadingDirection.North, PlayerPosition.PitchDirection.Up);
                Blocks = new byte[XDim * YDim * ZDim];

                FillBlocks(0, 0, 0, XDim, 1, ZDim, Gameplay.Blocks.Lavastill);
                FillBlocks(0, 1, 0, XDim, 5, ZDim, Gameplay.Blocks.Stone);
                FillBlocks(0, 6, 0, XDim, 14, ZDim, Gameplay.Blocks.Dirt);
                FillBlocks(0, 15, 0, XDim, 1, ZDim, Gameplay.Blocks.Grass);
                FillBlocks(0, 16, 0, XDim, (short)(YDim - 16), ZDim, Gameplay.Blocks.Air);

                nBT.SetObject(string.Empty, new NBTCompound("MapGenerator", new List<NBTObject>()
                {
                    new NBTString("Software", "TeleClassic"),
                    new NBTString("MapGeneratorName", "Flat")
                })); 
                edited = true;
                Save();
            }
        }

        public void Save()
        {
            if (!edited)
                return;
            nBT.SetObject(string.Empty, new NBTString("Name", Name));
            nBT.SetObject(string.Empty, new NBTByte("FormatVersion", 1));

            nBT.SetObject(string.Empty, new NBTShort("X", XDim));
            nBT.SetObject(string.Empty, new NBTShort("Y", YDim));
            nBT.SetObject(string.Empty, new NBTShort("Z", ZDim));

            BlockPosition blockSpawn = new BlockPosition(SpawnPoint);

            nBT.SetObject(string.Empty, new NBTCompound("Spawn",
                new List<NBTObject>(){
                new NBTShort("X", blockSpawn.X),
                new NBTShort("Y", blockSpawn.Y),
                new NBTShort("Z", blockSpawn.Z)
            }));

            nBT.SetObject(string.Empty, new NBTByteArray("BlockArray", Blocks));
            nBT.Save();
            Logger.Log("Info", "Saved world.", Name);
            this.edited = false;
        }

        public bool InWorld(BlockPosition position) => position.X >= 0 && position.X < XDim && position.Y >= 0 && position.Y < YDim && position.Z >= 0 && position.Z < ZDim;

        public bool InWorld(PlayerPosition position) => InWorld(new BlockPosition(position));

        public virtual void SetBlock(BlockPosition position, byte blockType)
        {
            if (!InWorld(position))
                throw new InvalidOperationException("Set block position is out of bounds.");
            this.edited = true;
            Blocks[IndexFromPosition(position)] = blockType;
        }

        public byte GetBlock(BlockPosition position)
        {
            if (!InWorld(position))
                throw new InvalidOperationException("Set block position is out of bounds.");
            return Blocks[IndexFromPosition(position)];
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        public void SendWorld(World world)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                SendPacket(new LevelInitializePacket());
                using (GZipStream gzip = new GZipStream(buffer, CompressionMode.Compress, true))
                using (BinaryWriter writer = new BinaryWriter(gzip, Encoding.UTF8, true))
                {
                    writer.Write(IPAddress.HostToNetworkOrder(world.Blocks.Length));
                    gzip.Write(world.Blocks, 0, world.Blocks.Length);
                }

                byte[] world_data = buffer.GetBuffer();
                for (int i = 0; i < buffer.Length; i = i + 1024)
                {
                    byte[] chunk = new byte[1024];
                    short copied;
                    for (copied = 0; copied < 1024 && i + copied < buffer.Length; copied++)
                        chunk[copied] = world_data[i + copied];
                    SendPacket(new LevelDataChunkPacket(copied, chunk, 0));
                }

                SendPacket(new LevelFinalizePacket(world.XDim, world.YDim, world.ZDim));
            }
        }
    }
}