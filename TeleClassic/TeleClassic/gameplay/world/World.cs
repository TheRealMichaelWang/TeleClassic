using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.IO;
using TeleClassic.gameplay.world;
using TeleClassic.networking.protocol.clientbound;
using System.Net;

namespace TeleClassic.gameplay
{
    partial class Player
    {
        public void SendWorldData(World world)
        {
            ParentSession.SendPacket(new LevelInitializePacket());
            using (MemoryStream compressed = new MemoryStream())
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    stream.Write(BitConverter.GetBytes((int)IPAddress.HostToNetworkOrder(world.BlockArray.Length)), 0, 4);
                    stream.Write(world.BlockArray, 0, world.BlockArray.Length);
                    using(GZipStream gzip = new GZipStream(compressed, CompressionMode.Compress))
                    {
                        stream.WriteTo(gzip);
                    }
                }
                byte[] world_data = compressed.GetBuffer();
                for (int i = 0; i < world_data.Length; i = i + 1024)
                {
                    byte[] chunk = new byte[1024];
                    short copied;
                    for (copied = 0; copied < 1024 && i + copied < world_data.Length; copied++)
                    {
                        chunk[copied] = world_data[i + copied];
                    }
                    ParentSession.SendPacket(new LevelDataChunkPacket(copied, chunk, 0));
                }
            }
            ParentSession.SendPacket(new LevelFinalizePacket(world.XLimit, world.YLimit, world.ZLimit));
        }
    }
}

namespace TeleClassic.gameplay.world
{
    public partial class World
    {
        public static World FromFile(string filePath)
        {
            return FromData(File.ReadAllBytes(filePath));
        }

        public static World FromData(byte[] data)
        {
            World world;
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (GZipStream gZip = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        gZip.CopyTo(decompressedStream);
                    }
                }
                decompressedStream.Position = 0;
                world = new World(decompressedStream);
            }
            return world;
        }

        public byte this[short x, short y, short z]
        { 
            get
            {
                return GetBlock(x, y, z);
            }
            set
            {
                SetBlock(x, y, z, value);
            }
        }

        NBT NBT;
        public byte[] BlockArray { get; private set; }

        public readonly byte[] UUID;

        public readonly short XLimit;
        public readonly short YLimit;
        public readonly short ZLimit;

        public readonly string Name;

        public Position SpawnPoint;

        public World(MemoryStream stream)
        {
            NBT = new NBT(stream);
            Name = NBT.HasObject("ClassicWorld/Name") ? (string)NBT.GetObject("ClassicWorld/Name") : "UntitledWorld";
            UUID = (byte[])NBT.GetObject("ClassicWorld/UUID");
            BlockArray = (byte[])NBT.GetObject("ClassicWorld/BlockArray");
            XLimit = (short)NBT.GetObject("ClassicWorld/X");
            YLimit = (short)NBT.GetObject("ClassicWorld/Y");
            ZLimit = (short)NBT.GetObject("ClassicWorld/Z");
            SpawnPoint = new Position((short)((short)NBT.GetObject("ClassicWorld/Spawn/X") * 32), (short)((short)NBT.GetObject("ClassicWorld/Spawn/Y") * 32 + 51), (short)((short)NBT.GetObject("ClassicWorld/Spawn/Z") * 32), (byte)NBT.GetObject("ClassicWorld/Spawn/H"), (byte)NBT.GetObject("ClassicWorld/Spawn/P"));
        }

        public World(byte[] uuid, string name, short xLimit, short yLimit, short zLimit, byte[] blockArray, Position spawnPoint)
        {
            this.UUID = uuid;
            this.Name = name;
            this.XLimit = xLimit;
            this.YLimit = yLimit;
            this.ZLimit = zLimit;
            this.BlockArray = blockArray;
            this.SpawnPoint = spawnPoint;
            NBT = new NBT();
            NBT.AddCompound("","ClassicWorld");
            NBT.AddValue("ClassicWorld", "FormatVersion", 1, 1);
            NBT.AddValue("ClassicWorld", "UUID", 7, uuid);
            NBT.AddValue("ClassicWorld", "Name", 8, name);
            NBT.AddValue("ClassicWorld", "BlockArray", 7, blockArray);
            NBT.AddValue("ClassicWorld", "X", 2, xLimit);
            NBT.AddValue("ClassicWorld", "Y", 2, yLimit);
            NBT.AddValue("ClassicWorld", "Z", 2, zLimit);
            NBT.AddCompound("ClassicWorld", "Spawn");
            NBT.AddValue("ClassicWorld/Spawn", "X", 2, spawnPoint.X);
            NBT.AddValue("ClassicWorld/Spawn", "Y", 2, spawnPoint.Y);
            NBT.AddValue("ClassicWorld/Spawn", "Z", 2, spawnPoint.Z);
            NBT.AddValue("ClassicWorld/Spawn", "H", 1, spawnPoint.Yaw);
            NBT.AddValue("ClassicWorld/Spawn", "P", 1, spawnPoint.Pitch);
            NBT.AddCompound("ClassicWorld","Metadata");
        }

        public void SetBlock(short x, short y, short z, byte block)
        {
            BlockArray[BlockIndexFromPosition(x, y, z)] = block;
        }

        public byte GetBlock(short x, short y, short z)
        {
            if(BlockIndexFromPosition(x, y, z) == -1)
            {
                return Blocks.Error;
            }
            return BlockArray[BlockIndexFromPosition(x, y, z)];
        }

        private int BlockIndexFromPosition(short x, short y, short z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= XLimit || y >= YLimit || z >= ZLimit)
            {
                return -1;
            }
            return (y * ZLimit + z) * XLimit + x;
        }

        public byte[] ToByteArray()
        {
            NBT.SetObject("ClassicWorld/UUID", UUID);
            NBT.SetObject("ClassicWorld/Name", Name);
            NBT.SetObject("ClassicWorld/BlockArray", BlockArray);
            NBT.SetObject("ClassicWorld/X", XLimit);
            NBT.SetObject("ClassicWorld/Y", YLimit);
            NBT.SetObject("ClassicWorld/Z", ZLimit);
            NBT.SetObject("ClassicWorld/Spawn/X", SpawnPoint.X / 32);
            NBT.SetObject("ClassicWorld/Spawn/Y", (SpawnPoint.Y - 51)/32);
            NBT.SetObject("ClassicWorld/Spawn/Z", SpawnPoint.Z / 32);
            NBT.SetObject("ClassicWorld/Spawn/H", SpawnPoint.Yaw);
            NBT.SetObject("ClassicWorld/Spawn/P", SpawnPoint.Pitch);
            byte[] data;
            using(MemoryStream tocompress = new MemoryStream())
            {
                NBT.Write(tocompress);
                byte[] datatocompress = tocompress.ToArray();
                using (MemoryStream compressedData = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(compressedData, CompressionMode.Compress))
                    {
                        gzip.Write(datatocompress, 0, datatocompress.Length);
                    }
                    data = compressedData.ToArray();
                }
            }
            return data;
        }

        public void SaveToFile(string filepath)
        {
            File.WriteAllBytes(filepath, ToByteArray());
        }
    }
}
