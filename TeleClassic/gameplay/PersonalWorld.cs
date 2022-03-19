using System;
using System.Collections.Generic;
using System.IO;
using TeleClassic.Networking;

namespace TeleClassic.Gameplay
{
    public sealed class PersonalWorld : MultiplayerWorld
    {
        public sealed class WorldEditor : IDisposable
        {
            public sealed class BlockSelection
            {
                public readonly BlockPosition Begin;
                public readonly BlockPosition End;

                public readonly MultiplayerWorld World;
                public readonly PlayerSession PlayerSession;

                public short XDim
                {
                    get => (short)(End.X - Begin.X);
                }

                public short YDim
                {
                    get => (short)(End.Y - Begin.Y);
                }

                public short ZDim
                {
                    get => (short)(End.Z - Begin.Z);
                }

                public BlockSelection(BlockPosition begin, BlockPosition end, MultiplayerWorld world, PlayerSession playerSession)
                {
                    this.Begin = new BlockPosition(Math.Min(begin.X, end.X), Math.Min(begin.Y, end.Y), Math.Min(begin.Z, end.Z));
                    this.End = new BlockPosition(Math.Max(begin.X, end.X), Math.Max(begin.Y, end.Y), Math.Max(begin.Z, end.Z));
                    this.World = world;
                    this.PlayerSession = playerSession;
                }

                public BlockSelection(BlockPosition begin, BlockPosition end, WorldEditor worldEditor) : this(begin, end, worldEditor.World, worldEditor.PlayerSession){}

                public bool WithinRange(BlockPosition blockPosition) => blockPosition.X >= Begin.X && blockPosition.Y >= Begin.Y && blockPosition.Z >= Begin.Z && blockPosition.X <= End.X && blockPosition.Y <= End.Y && blockPosition.Z <= End.Z;

                public void Highlight(byte highlightBlockType)
                {
                    for (short x = Begin.X; x <= End.X; x++)
                        for (short y = Begin.Y; y <= End.Y; y++)
                            for (short z = Begin.Z; z <= End.Z; z++)
                                PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(new BlockPosition(x, y, z), highlightBlockType));
                }

                public void Unhilight()
                {
                    for (short x = Begin.X; x <= End.X; x++)
                        for (short y = Begin.Y; y <= End.Y; y++)
                            for (short z = Begin.Z; z <= End.Z; z++)
                                PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(new BlockPosition(x, y, z), World.GetBlock(new BlockPosition(x, y, z))));
                }
            }

            public sealed class BeginSelectBlocksCommandAction : CommandProcessor.CommandAction
            {
                public int GetExpectedArgumentCount() => 0;
                public bool ReturnsValue() => false;

                public string GetName() => "select";
                public string GetDescription() => "Begins the block selection process w/ world editor.";

                WorldEditor worldEditor;

                public BeginSelectBlocksCommandAction(WorldEditor worldEditor)
                {
                    this.worldEditor = worldEditor;
                }

                public void Invoke(CommandProcessor commandProcessor)
                {
                    if (this.worldEditor.selectionMode)
                    {
                        commandProcessor.Print("You have already entered the world-editor block select process.");
                        return;
                    }
                    this.worldEditor.selectionMode = true;
                    commandProcessor.Print("You have started the world-editor select block process;\n"
                                            + "- To select a begin/end range block, place a block.\n"
                                            + "- To cancel the process break any blolck.");
                }
            }

            public readonly MultiplayerWorld World;
            public readonly PlayerSession PlayerSession;

            BlockSelection currentSelection;
            BlockPosition selectedPosition1;
            BlockPosition selectedPosition2;
            bool selectionMode;

            bool disposed;

            public WorldEditor(MultiplayerWorld world, PlayerSession playerSession)
            {
                this.World = world;
                this.PlayerSession = playerSession;
                this.selectionMode = false;
                this.disposed = false;

                playerSession.CommandParser.AddCommand(new BeginSelectBlocksCommandAction(this));
            }

            public bool WithinCurrentSelection(BlockPosition blockPosition)
            {
                if (this.currentSelection == null)
                    return false;
                return this.currentSelection.WithinRange(blockPosition);
            }

            public void Select(BlockSelection blockSelection)
            {
                if (blockSelection.World != World || blockSelection.PlayerSession != PlayerSession)
                    throw new InvalidOperationException("Cannot use a selection from another world/player.");
                Deselect();
                currentSelection = blockSelection;
                currentSelection.Highlight(Gameplay.Blocks.Waterstill);
            }
            public BlockSelection Select(BlockPosition begin, BlockPosition end)
            {
                BlockSelection selection = new BlockSelection(begin, end, this);
                Select(selection);
                return selection;
            }

            public void Deselect()
            {
                if (currentSelection != null)
                    currentSelection.Unhilight();
                currentSelection = null;
            }

            public bool SetBlock(BlockPosition position, byte blockType)
            {
                if (selectionMode)
                {
                    if (blockType != Gameplay.Blocks.Air)
                    {
                        if(selectedPosition1 == null)
                        {
                            selectedPosition1 = position;
                            PlayerSession.Message("You have selected block position 1. Please select another position.");
                            return false;
                        }
                        else
                        {
                            selectedPosition2 = position;
                            Select(selectedPosition1, selectedPosition2);
                        }
                    }
                    this.selectionMode = false;
                    selectedPosition1 = null;
                    selectedPosition2 = null;
                    return false;
                }
                return true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    disposed = true;
                    if (disposing)
                    {
                        PlayerSession.CommandParser.RemoveCommand("select");
                        Deselect();
                    }
                }
            }
        }

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
                worldEditorInstances.Add(playerSession, new WorldEditor(this, playerSession));
            base.JoinWorld(playerSession);
        }

        public override void LeaveWorld(PlayerSession playerSession)
        {
            if (CanBuild(playerSession))
                worldEditorInstances.Remove(playerSession);
            base.LeaveWorld(playerSession);
        }

        public override void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (!CanBuild(playerSession))
            {
                playerSession.Message("You cannot build in another's players personal world.");
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
