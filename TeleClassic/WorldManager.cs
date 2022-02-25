using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.Gameplay;
using TeleClassic.Networking;

namespace TeleClassic
{
    public sealed class WorldManager
    {
        public sealed class GetWorldListCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => true;

            public string GetName() => "lw";
            public string GetDescription() => "Lists all worlds on the server.";

            WorldManager worldManager;

            public GetWorldListCommandAction(WorldManager worldManager)
            {
                this.worldManager = worldManager;
            }

            public void Invoke(CommandProcessor commandProcessor) => commandProcessor.PushObject(new CommandProcessor.WorldCommandObject(worldManager.worldLookup.Values.ToList()));
        }

        public sealed class GeneratePersonalWorldCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => true;

            public string GetName() => "gpw";
            public string GetDescription() => "Creates a new personal world.";

            WorldManager worldManager;

            public GeneratePersonalWorldCommandAction(WorldManager worldManager)
            {
                this.worldManager = worldManager;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                if (commandProcessor.Permissions < Permission.Operator)
                    throw new ArgumentException("You must be an operator or admin to create personal worlds.");

                CommandProcessor.StringCommandObject worldId = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                CommandProcessor.PlayerCommandObject newPlayerOwner = (CommandProcessor.PlayerCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.PlayerCommandObject));
                if (newPlayerOwner.playerSessions.Count != 1)
                    throw new ArgumentException("Expected 1 owner got " + newPlayerOwner.playerSessions.Count + " player(s).");
                if (this.worldManager.HasWorld(worldId.String))
                    throw new ArgumentException("World \"" + worldId.String + "\" has already been.");
                PersonalWorld personalWorld = new PersonalWorld("worlds/" + worldId.String, newPlayerOwner.playerSessions[0].Account, true); 
                worldManager.AddPersonalWorld(personalWorld);
                List<MultiplayerWorld> createdWorlds = new List<MultiplayerWorld>();
                createdWorlds.Add(personalWorld);
                commandProcessor.PushObject(new CommandProcessor.WorldCommandObject(createdWorlds));
            }
        }

        public sealed class PersonalWorld : MultiplayerWorld
        {
            public Account Owner;
            public bool IsPublic;
            public DateTime LastEdit;
            public int BlocksPlaced;
            public int BlocksBroken;

            public PersonalWorld(string fileName, Account owner, bool isPublic) : base(fileName, Permission.Member, Permission.Member, MultiplayerWorld.MaxPlayerCapacity)
            {
                this.Owner = owner;
                this.IsPublic = isPublic;
                this.LastEdit = DateTime.Now;
                this.BlocksPlaced = 0;
                this.BlocksBroken = 0;
            }

            public PersonalWorld(BinaryReader reader, AccountManager accountManager) : base(reader.ReadString(), Permission.Member, Permission.Member, MultiplayerWorld.MaxPlayerCapacity)
            {
                string ownerUsername = reader.ReadString();
                if (ownerUsername == "ARCHIVED")
                {
                    this.Owner = null;
                    this.IsPublic = reader.ReadBoolean();
                }
                else if (accountManager.UserExists(ownerUsername))
                {
                    this.Owner = accountManager.FindUser(ownerUsername);
                    this.IsPublic = reader.ReadBoolean();
                }
                else
                {
                    Logger.Log("Info", "World no longer has owner.", this.Name);
                    if(this.BlocksPlaced >= 1000)
                    {
                        Logger.Log("Info", "Archiving world because it has more than 1000 placed blocks.", this.Name);
                        this.Owner = null;
                        this.IsPublic = true;
                        reader.ReadBoolean();
                    }
                    else
                    {
                        Logger.Log("info", "Deleting world because it's insignifigant and has no owner.", this.Name);
                        File.Delete(this.Name);
                        throw new ArgumentException("Owner of world deleted their account.");
                    }
                }
                this.LastEdit = new DateTime(reader.ReadInt64());
                this.BlocksPlaced = reader.ReadInt32();
                this.BlocksBroken = reader.ReadInt32();
            }

            public override void JoinWorld(PlayerSession playerSession)
            {
                if (!IsPublic && !(playerSession.Account == Owner || playerSession.Permissions == Permission.Admin))
                    throw new InvalidOperationException("The owner set this personal world to private.");
                base.JoinWorld(playerSession);
            }

            public override void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
            {
                if(!((playerSession.IsLoggedIn && playerSession.Account == Owner) || playerSession.Permissions == Permission.Admin))
                {
                    playerSession.Message("You cannot build in another's players personal world.");
                    return;
                }
                base.SetBlock(playerSession, position, blockType);
                if (blockType == Gameplay.Blocks.Air)
                    this.BlocksBroken++;
                else
                    this.BlocksPlaced++;
                this.LastEdit = DateTime.Now;
            }

            public void TransferOwnership(Account newOwner)
            {
                Logger.Log("Info", "World \"" + this.Name + "\" transfered ownership", newOwner.Username);
                this.Owner = newOwner;
            }

            public void WriteBack(BinaryWriter writer)
            {
                if(this.Owner == null)
                    writer.Write("ARCHIVED");
                writer.Write(this.IsPublic);
                writer.Write(this.LastEdit.Ticks);
                writer.Write(this.BlocksPlaced);
                writer.Write(this.BlocksBroken);
            }
        }

        public MultiplayerWorld Lobby { get; private set; }

        private Dictionary<string, MultiplayerWorld> worldLookup;
        private List<PersonalWorld> personalWorlds;
        private string personalWorldsDbFile;

        public bool HasWorld(string worldName) => worldLookup.ContainsKey(worldName);

        public void AddWorld(MultiplayerWorld world) => worldLookup.Add(world.Name.StartsWith("worlds/") ? world.Name.Substring("worlds/".Length) : world.Name, world);

        public WorldManager(MultiplayerWorld lobby, AccountManager accountManager, string personalWorldsDbFile)
        {
            this.Lobby = lobby;
            this.personalWorldsDbFile = personalWorldsDbFile;
            worldLookup = new Dictionary<string, MultiplayerWorld>();
            worldLookup.Add("lobby", lobby);

            if (!Directory.Exists("worlds"))
                Directory.CreateDirectory("worlds");

            Logger.Log("Info", "Loading personal worlds.", "None");
            if (File.Exists(personalWorldsDbFile))
            {
                using (FileStream fileStream = new FileStream(personalWorldsDbFile, FileMode.Open, FileAccess.Read))
                using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Decompress))
                using (BinaryReader binaryReader = new BinaryReader(gZip))
                {
                    int personalWorldCount = binaryReader.ReadInt32();
                    int cleanedWorlds = 0;
                    personalWorlds = new List<PersonalWorld>(personalWorldCount);
                    for (int i = 0; i < personalWorldCount; i++)
                        try
                        {
                            PersonalWorld personalWorld = new PersonalWorld(binaryReader, accountManager);
                            AddPersonalWorld(personalWorld);
                        }
                        catch (ArgumentException)
                        {
                            cleanedWorlds++;
                        }
                    Logger.Log("Info", cleanedWorlds + " personal worlds cleaned.", "None");
                }
            }
            else
            {
                personalWorlds = new List<PersonalWorld>();
                File.Create(personalWorldsDbFile).Close();
            }
        }

        public void AddPersonalWorld(PersonalWorld personalWorld)
        {
            personalWorlds.Add(personalWorld);
            AddWorld(personalWorld);
        }

        public void Save()
        {
            Logger.Log("Info", "Saving Personal Worlds", "None");
            using (FileStream fileStream = new FileStream(personalWorldsDbFile, FileMode.Open, FileAccess.Write))
            using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Compress))
            using (BinaryWriter binaryWriter = new BinaryWriter(gZip))
            {
                binaryWriter.Write(personalWorlds.Count);
                foreach (PersonalWorld personalWorld in personalWorlds)
                    personalWorld.WriteBack(binaryWriter);
            }
            foreach (MultiplayerWorld world in worldLookup.Values)
                world.Save();
        }
    }
}
