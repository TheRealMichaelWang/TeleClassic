using System;
using System.Collections.Generic;
using System.Drawing;
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
        public struct CustomBlockDefinition
        {
            public enum BlockSolidity : byte
            {
                WalkThrough = 0,
                SwimThrough = 1,
                Solid = 2,
                PartiallySlippery = 3,
                FullySlippery = 4,
                LikeLava = 5,
                LikeWater = 6,
                LikeLadder = 7
            }

            public enum WalkSound : byte
            {
                NoSound = 0,
                Wood = 1,
                Gravel = 2,
                Grass = 3,
                Stone = 4,
                Netal = 5,
                Glass = 6,
                Wool = 6,
                Sand = 8,
                Snow = 9
            }

            public enum BlockDraw : byte
            {
                FullyOpaque = 0,
                Transparent = 1,
                TransparentNoCulling = 2,
                Translucent = 3,
                Gas = 4
            }

            public struct BlockTextureInfo
            {
                public readonly byte TopTextureID;
                public readonly byte SideTextureID;
                public readonly byte BottomTextureID;

                public BlockTextureInfo(byte topTextureID, byte sideTextureID, byte bottomTextureID)
                {
                    this.TopTextureID = topTextureID;
                    this.SideTextureID = sideTextureID;
                    this.BottomTextureID = bottomTextureID;
                }

                public BlockTextureInfo(NBTByteArray nBTByteArray)
                {
                    this.TopTextureID = nBTByteArray.Data[0];
                    this.BottomTextureID = nBTByteArray.Data[1];
                    this.SideTextureID = nBTByteArray.Data[2];
                }

                public NBTByteArray GetNBTByteArray()
                {
                    return new NBTByteArray("Textures", new byte[] { this.TopTextureID, this.BottomTextureID, this.SideTextureID });
                }
            }

            public readonly byte BlockID;
            public readonly string Name;
            public readonly BlockSolidity Solidity;
            public readonly float MovementSpeed;
            public readonly BlockTextureInfo TextureInfo;
            public readonly bool TransmitsLight;
            public readonly WalkSound Sound;
            public readonly bool FullBright;
            public readonly byte Shape;
            public readonly BlockDraw DrawMode;
            public readonly byte FogDensity;
            public readonly Color FogColor;

            public CustomBlockDefinition(NBTCompound nBTCompound)
            {
                this.BlockID = (byte)nBTCompound.FindChild("ID").GetPayload();
                this.Name = (string)nBTCompound.FindChild("Name").GetPayload();
                this.MovementSpeed = (float)nBTCompound.FindChild("Speed").GetPayload();
                this.TextureInfo = new BlockTextureInfo((NBTByteArray)nBTCompound.FindChild("Textures"));
                this.TransmitsLight = ((byte)nBTCompound.FindChild("TransmitsLight").GetPayload()) == 1;
                this.Sound = Enum.Parse<WalkSound>(nBTCompound.FindChild("WalkSound").GetPayload().ToString());
                this.Shape = (byte)nBTCompound.FindChild("Shape").GetPayload();
                this.DrawMode = Enum.Parse<BlockDraw>(nBTCompound.FindChild("BlockDraw").GetPayload().ToString());
                NBTByteArray fogDataArray = (NBTByteArray)nBTCompound.FindChild("Fog");
                this.FogDensity = fogDataArray.Data[0];
                this.FogColor = Color.FromArgb(fogDataArray.Data[1], fogDataArray.Data[2], fogDataArray.Data[3]);
                this.Solidity = BlockSolidity.Solid;
                this.FullBright = true;
            }

            public NBTCompound GetNBTCompound()
            {
                List<NBTObject> children = new List<NBTObject>(10);
                children.Add(new NBTByte("ID", this.BlockID));
                children.Add(new NBTString("Name", this.Name));
                children.Add(new NBTFloat("Speed", this.MovementSpeed));
                children.Add(this.TextureInfo.GetNBTByteArray());
                children.Add(new NBTByte("TransmitsLight", this.TransmitsLight ? (byte)1 : (byte)0));
                children.Add(new NBTByte("WalkSound", (byte)this.Sound));
                children.Add(new NBTByte("Shape", this.Shape));
                children.Add(new NBTByte("BlockDraw", (byte)this.DrawMode));
                children.Add(new NBTByteArray("Fog", new byte[] { this.FogDensity, this.FogColor.R, this.FogColor.G, this.FogColor.B }));
                return new NBTCompound("Block" + this.BlockID, children);
            }
        }

        public struct EnvironmentConfiguration
        {
            public enum WeatherType
            {
                Sunny = 0,
                Raining = 1,
                Snowing = 2
            }

            public string TextureUrl;
            public byte SideBlock;
            public byte EdgeBlock;
            public short SideLevel;

            public WeatherType Weather;

            public EnvironmentConfiguration(string textureURL, byte sideBlock, byte edgeBlock, short sideLevel, WeatherType weather)
            {
                this.TextureUrl = textureURL;
                this.SideBlock = sideBlock;
                this.EdgeBlock = edgeBlock;
                this.SideLevel = sideLevel;
                this.Weather = weather;
            }
        }

        public string Name { get; protected set; }
        private string fileName;

        public PlayerPosition SpawnPoint;
        public List<CustomBlockDefinition> customBlockDefinitions;
        public EnvironmentConfiguration environmentConfiguration;

        protected NBT nBT;

        public byte[] Blocks;

        public readonly short XDim;
        public readonly short YDim;
        public readonly short ZDim;

        public readonly byte FormatVersion; //who gives a fuck?

        protected int IndexFromPosition(BlockPosition position) => (position.Y * ZDim + position.Z) * XDim + position.X;
        protected bool edited;

        protected void FillBlocks(short x, short y, short z, short xDim, short yDim, short zDim, byte blockType)
        {
            for(short cx = x; cx < x + xDim; cx++)
                for(short cy = y; cy < y + yDim; cy++)
                    for(short cz = z; cz < z + zDim; cz++)
                        this.Blocks[(cy * this.ZDim + cz) * this.XDim + cx] = blockType;
        }

        public World(string name, string fileName)
        {
            this.Name = name;
            this.fileName = fileName;
            nBT = new NBT(fileName);

            this.environmentConfiguration = new EnvironmentConfiguration("https://bit.ly/3NSmdgu", Gameplay.Blocks.Water, Gameplay.Blocks.Bedrock, 16, EnvironmentConfiguration.WeatherType.Sunny);
            try
            {
                //read a world 
                FormatVersion = (byte)nBT.FindObject("FormatVersion");

                XDim = (short)nBT.FindObject("X");
                YDim = (short)nBT.FindObject("Y");
                ZDim = (short)nBT.FindObject("Z");

                SpawnPoint = new PlayerPosition(new BlockPosition((short)nBT.FindObject("Spawn.X"), (short)nBT.FindObject("Spawn.Y"), (short)nBT.FindObject("Spawn.Z")), PlayerPosition.HeadingDirection.North, PlayerPosition.PitchDirection.Up);

                Blocks = (byte[])nBT.FindObject("BlockArray");

                if (nBT.ObjectExists("Metadata.CPE"))
                {
                    NBTCompound CPEMetadata = (NBTCompound)nBT.FindObject("Metadata.CPE");
                    if (CPEMetadata.HasChild("EnvMapAppearance"))
                    {
                        NBTCompound envMapAppearanceMetadata = (NBTCompound)CPEMetadata.FindChild("EnvMapAppearance");
                        string req_texture = (string)envMapAppearanceMetadata.FindChild("TextureURL").GetPayload();
                        if (!string.IsNullOrEmpty(req_texture))
                            environmentConfiguration.TextureUrl = req_texture; 
                        environmentConfiguration.SideBlock = (byte)envMapAppearanceMetadata.FindChild("SideBlock").GetPayload();
                        environmentConfiguration.EdgeBlock = (byte)envMapAppearanceMetadata.FindChild("EdgeBlock").GetPayload();
                        environmentConfiguration.SideLevel = (short)envMapAppearanceMetadata.FindChild("SideLevel").GetPayload();
                    }
                    if (CPEMetadata.HasChild("EnvWeatherType"))
                    {
                        NBTCompound envWeatherMetadata = (NBTCompound)CPEMetadata.FindChild("EnvWeatherType");
                        environmentConfiguration.Weather = Enum.Parse<EnvironmentConfiguration.WeatherType>(envWeatherMetadata.FindChild("WeatherType").GetPayload().ToString());
                    }
                    if (CPEMetadata.HasChild("BlockDefinitions"))
                    {
                        NBTCompound blockDefinitions = (NBTCompound)CPEMetadata.FindChild("BlockDefinitions");
                        this.customBlockDefinitions = new List<CustomBlockDefinition>(blockDefinitions.Children.Count - 1);

                        foreach (NBTObject nBTObject in blockDefinitions.Children)
                            if (nBTObject.Name.StartsWith("Block"))
                            {
                                NBTCompound blockDefinition = (NBTCompound)nBTObject;
                                this.customBlockDefinitions.Add(new CustomBlockDefinition(blockDefinition));
                            }
                    }
                }

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
                this.customBlockDefinitions = new List<CustomBlockDefinition>();

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
        HashSet<byte> supportedCustomBlocks = new HashSet<byte>();

        public bool SupportsBlock(byte block)
        {
            if (this.ExtensionManager.SupportsExtension("BlockDefinitions"))
                return supportedCustomBlocks.Contains(block);
            else if (!this.ExtensionManager.SupportsExtension("CustomBlocks"))
                return block <= 50;
            else if (this.ExtensionManager.CustomBlockSupportLevel == 1)
                return block <= 65;
            throw new InvalidOperationException();
        }

        public void SendWorld(World world)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                SendPacket(new LevelInitializePacket());

                if (this.ExtensionManager.SupportsExtension("EnvMapAspect") && this.ExtensionManager.SupportsExtension("EnvWeatherType"))
                {
                    SendPacket(new SetMapEnvUrlPacket(world.environmentConfiguration.TextureUrl));
                    SendPacket(new EnvSetWeatherTypePacket(world.environmentConfiguration.Weather));
                    SendPacket(new SetMapEnvPropertyPacket(SetMapEnvPropertyPacket.PropertyType.EdgeBlockType, world.environmentConfiguration.EdgeBlock));
                    SendPacket(new SetMapEnvPropertyPacket(SetMapEnvPropertyPacket.PropertyType.SideBlockType, world.environmentConfiguration.SideBlock));
                    SendPacket(new SetMapEnvPropertyPacket(SetMapEnvPropertyPacket.PropertyType.MapEdgeHeight, world.environmentConfiguration.SideLevel));
                }
                else if (this.ExtensionManager.SupportsExtension("EnvMapAppearance"))
                {
                    if (this.ExtensionManager.GetExtensionVersion("EnvMapAppearance") == 2)
                    {
                        
                    }
                    else
                    {

                    }
                }
                if (this.ExtensionManager.SupportsExtension("BlockDefinitions"))
                {
                    foreach (World.CustomBlockDefinition customBlockDefinition in world.customBlockDefinitions)
                    {
                        SendPacket(new DefineBlockPacket(customBlockDefinition));
                        supportedCustomBlocks.Add(customBlockDefinition.BlockID);
                    }
                }

                using (GZipStream gzip = new GZipStream(buffer, CompressionMode.Compress, true))
                using (BinaryWriter writer = new BinaryWriter(gzip, Encoding.UTF8, true))
                {
                    writer.Write(IPAddress.HostToNetworkOrder(world.Blocks.Length));

                    if(this.ExtensionManager.SupportsExtension("CustomBlocks") && this.ExtensionManager.CustomBlockSupportLevel == ExtendedBlocks.MaxSupportLevel)
                        gzip.Write(world.Blocks, 0, world.Blocks.Length);
                    else
                    {
                        foreach (byte block in world.Blocks)
                            if (SupportsBlock(block))
                                gzip.WriteByte(block);
                            else
                            {
                                if (supportedCustomBlocks.Contains(block))
                                    gzip.WriteByte(Gameplay.Blocks.Air);
                                else
                                    gzip.WriteByte(ExtendedBlocks.GetExtendedBlockFallback(block));
                            }
                    }
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

        public void RemoveCustomBlockDefinitions()
        {
            this.supportedCustomBlocks.Clear();
            foreach (byte customBlockDeclartionID in this.supportedCustomBlocks)
                SendPacket(new RemoveSelectionPacket(customBlockDeclartionID));
        }
    }
}