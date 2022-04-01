using System;
using System.Collections.Generic;
using System.IO;
using TeleClassic.Networking;
using TeleClassic.Networking.Clientbound;
using TeleClassic.Networking.Serverbound;

namespace TeleClassic.Gameplay
{
    public sealed partial class PersonalWorld : MultiplayerWorld
    {
        public Account Owner;
        public bool IsPublic;
        public DateTime LastEdit;
        public int BlocksPlaced;
        public int BlocksBroken;

        Dictionary<PlayerSession, WorldEditor> worldEditorInstances;
        public bool CanBuild(PlayerSession playerSession) => ((playerSession.IsLoggedIn && playerSession.Account == Owner) || playerSession.Permissions == Permission.Admin);

        public PersonalWorld(string fileName, Account owner, bool isPublic) : base(fileName, Permission.Member, Permission.Member, MultiplayerWorld.MaxPlayerCapacity)
        {
            this.Owner = owner;
            this.IsPublic = isPublic;
            this.LastEdit = DateTime.Now;
            this.BlocksPlaced = 0;
            this.BlocksBroken = 0;
            this.worldEditorInstances = new Dictionary<PlayerSession, WorldEditor>();
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
                if (this.BlocksPlaced >= 1000)
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
            this.worldEditorInstances = new Dictionary<PlayerSession, WorldEditor>();
        }

        public override void JoinWorld(PlayerSession playerSession)
        {
            if (!IsPublic && !CanBuild(playerSession))
                throw new InvalidOperationException("The owner set this personal world to private.");
            if (CanBuild(playerSession))
            {
                worldEditorInstances.Add(playerSession, new WorldEditor(this, playerSession));
                playerSession.Announce("Welcome Back!");
            }
            else
            {
                if (this.Owner == null)
                    playerSession.Announce("Welcome to " + this.Name + "!");
                else
                    playerSession.Announce("Welcome to " + this.Owner.Username + "'s " + this.Name +"!");
            }
            if (playerSession.ExtensionManager.SupportsExtension("MessageTypes"))
            {
                playerSession.SendPacket(new MessagePacket(1, "Blocks Placed: " + this.BlocksPlaced));
                playerSession.SendPacket(new MessagePacket(2, "Blocks Destroyed: " + this.BlocksBroken));
                playerSession.SendPacket(new MessagePacket(3, "Is Public: " + this.IsPublic));

                playerSession.SendPacket(new MessagePacket(13, this.Name));
                if (this.Owner == null)
                    playerSession.SendPacket(new MessagePacket(12, "Owned By: Server"));
                else
                    playerSession.SendPacket(new MessagePacket(12, "Owned By: " + this.Owner.Username));
                playerSession.SendPacket(new MessagePacket(11, "Last Edit: " + this.LastEdit.ToShortDateString()));
            }
            base.JoinWorld(playerSession, PlayerJoinMode.Spectator);
            if(playerSession.ExtensionManager.SupportsExtension("HackControl"))
                playerSession.SendPacket(new HackControlPacket(true, true, true, true, true, 500));
        }

        public override void LeaveWorld(PlayerSession playerSession)
        {
            if (CanBuild(playerSession))
            {
                worldEditorInstances[playerSession].Dispose();
                worldEditorInstances.Remove(playerSession);
            }
            playerSession.ClearPersistantMessages();
            base.LeaveWorld(playerSession);
        }

        public override void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (playerSession.ExtensionManager.SupportsExtension("MessageTypes"))
            {
                this.MessageAllPlayers(new MessagePacket(1, "Blocks Placed: " + this.BlocksPlaced));
                this.MessageAllPlayers(new MessagePacket(2, "Blocks Destroyed: " + this.BlocksBroken));
                this.MessageAllPlayers(new MessagePacket(11, "Last Edit: " + this.LastEdit.ToShortDateString()));
            }

            if (!CanBuild(playerSession))
            {
                playerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(position, GetBlock(position)));
                playerSession.Message("You cannot build in another's players personal world.", false);
                return;
            }

            if (worldEditorInstances[playerSession].SetBlock(position, blockType))
            {
                base.SetBlock(playerSession, position, blockType);
                if (blockType == Gameplay.Blocks.Air)
                    this.BlocksBroken++;
                else
                    this.BlocksPlaced++;
                this.LastEdit = DateTime.Now;
            }
        }

        public void TransferOwnership(Account newOwner)
        {
            Logger.Log("Info", "World \"" + this.Name + "\" transfered ownership", newOwner.Username);
            this.Owner = newOwner;
        }

        public void WriteBack(BinaryWriter writer)
        {
            writer.Write(this.Name);
            if (this.Owner == null)
                writer.Write("ARCHIVED");
            else
                writer.Write(this.Owner.Username);
            writer.Write(this.IsPublic);
            writer.Write(this.LastEdit.Ticks);
            writer.Write(this.BlocksPlaced);
            writer.Write(this.BlocksBroken);
        }
    }
}
