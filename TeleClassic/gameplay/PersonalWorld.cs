using System;
using System.IO;
using TeleClassic.Networking;

namespace TeleClassic.Gameplay
{
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
        }

        public override void JoinWorld(PlayerSession playerSession)
        {
            if (!IsPublic && !(playerSession.Account == Owner || playerSession.Permissions == Permission.Admin))
                throw new InvalidOperationException("The owner set this personal world to private.");
            base.JoinWorld(playerSession);
        }

        public override void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (!((playerSession.IsLoggedIn && playerSession.Account == Owner) || playerSession.Permissions == Permission.Admin))
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
