using System;
using System.IO;
using System.Text;
using TeleClassic.Networking;
using TeleClassic.Networking.Clientbound;
using TeleClassic.Networking.Serverbound;

namespace TeleClassic.Gameplay
{
    public partial class PersonalWorld
    {
        public sealed partial class WorldEditor : IDisposable
        {
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

                    if (worldEditor.PlayerSession.ExtensionManager.SupportsExtension("PlayerClick"))
                    {
                        commandProcessor.Print("You have started the world-editor select block process:\n"
                                                + "- To select position a, move and right click.\n"
                                                + "- To select position b, move and left click.\n"
                                                + "- To finalize and select, middle click.");
                    }
                    else
                    {
                        commandProcessor.Print("You have started the world-editor select block process:\n"
                                                + "- To select a begin/end range block, place a block.");
                    }
                    commandProcessor.Print("- To cancel the process type '.deselct' or CTRL + D.");
                }
            }

            public sealed class DeselectCommandAction : CommandProcessor.CommandAction
            {
                public int GetExpectedArgumentCount() => 0;
                public bool ReturnsValue() => false;

                public string GetName() => "deselect";
                public string GetDescription() => "Deselects the current selection.";

                WorldEditor worldEditor;

                public DeselectCommandAction(WorldEditor worldEditor)
                {
                    this.worldEditor = worldEditor;
                }

                public void Invoke(CommandProcessor commandProcessor)
                {
                    if (worldEditor.selectionMode)
                    {
                        worldEditor.PlayerSession.Message("You have quit the world edit selection process.", false);
                        worldEditor.QuitSelectionMode();
                    }
                    else if (worldEditor.currentSelection == null)
                        commandProcessor.Print("No selection to deselect.");
                    else
                        this.worldEditor.Deselect();
                }
            }

            public sealed class FillCommandAction : CommandProcessor.CommandAction
            {
                public int GetExpectedArgumentCount() => 0;
                public bool ReturnsValue() => false;

                public string GetName() => "fill";
                public string GetDescription() => "Fills a selection with the block you are holding.";

                WorldEditor worldEditor;

                public FillCommandAction(WorldEditor worldEditor)
                {
                    this.worldEditor = worldEditor;
                }

                public void Invoke(CommandProcessor commandProcessor)
                {
                    if (worldEditor.currentSelection == null)
                    {
                        commandProcessor.Print("No selection to fill.");
                        return;
                    }
                    else if (!worldEditor.PlayerSession.ExtensionManager.SupportsExtension("HeldBlock"))
                    {
                        commandProcessor.Print("You must use a CPE compatible client w/ HeldBlock to use .fill");
                        return;
                    }

                    worldEditor.World.BeginBulkBlockUpdate();
                    for (short x = worldEditor.currentSelection.Begin.X; x <= worldEditor.currentSelection.End.X; x++)
                        for (short y = worldEditor.currentSelection.Begin.Y; y <= worldEditor.currentSelection.End.Y; y++)
                            for (short z = worldEditor.currentSelection.Begin.Z; z <= worldEditor.currentSelection.End.Z; z++)
                                this.worldEditor.World.SetBlock(new BlockPosition(x, y, z), worldEditor.PlayerSession.HeldBlock);
                    worldEditor.World.FinalizeBulkBlockUpdate();

                    worldEditor.Deselect();
                }
            }

            public sealed class BlockSelection
            {
                public readonly BlockPosition Begin;
                public readonly BlockPosition End;

                public readonly MultiplayerWorld World;
                public readonly PlayerSession PlayerSession;

                public bool Highlighted { get; private set; }

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
                    this.Highlighted = false;
                }

                public BlockSelection(BlockPosition begin, BlockPosition end, WorldEditor worldEditor) : this(begin, end, worldEditor.World, worldEditor.PlayerSession) { }

                public bool WithinRange(BlockPosition blockPosition) => blockPosition.X >= Begin.X && blockPosition.Y >= Begin.Y && blockPosition.Z >= Begin.Z && blockPosition.X <= End.X && blockPosition.Y <= End.Y && blockPosition.Z <= End.Z;

                public void Highlight(byte highlightBlockType)
                {
                    if (Highlighted)
                        throw new InvalidOperationException();

                    if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    {
                        PlayerSession.SendPacket(new MakeSelectionPacket(0, "selectedChunk", Begin, new BlockPosition(End, 1, 1, 1), 203, 195, 227, 80));
                    }
                    else
                    {
                        for (short x = Begin.X; x <= End.X; x++)
                            for (short y = Begin.Y; y <= End.Y; y++)
                                for (short z = Begin.Z; z <= End.Z; z++)
                                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(new BlockPosition(x, y, z), highlightBlockType));
                    }
                    Highlighted = true;
                }

                public void Unhilight()
                {
                    if (!Highlighted)
                        throw new InvalidOperationException();

                    if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    {
                        PlayerSession.SendPacket(new RemoveSelectionPacket(0));
                    }
                    else
                    {
                        for (short x = Begin.X; x <= End.X; x++)
                            for (short y = Begin.Y; y <= End.Y; y++)
                                for (short z = Begin.Z; z <= End.Z; z++)
                                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(new BlockPosition(x, y, z), World.GetBlock(new BlockPosition(x, y, z))));
                    }
                    Highlighted = false;
                }
            }

            static WorldEditor()
            {
                if (!Directory.Exists("structures"))
                    Directory.CreateDirectory("structures");
            }

            public readonly MultiplayerWorld World;
            public readonly PlayerSession PlayerSession;

            BlockSelection currentSelection;
            BlockPosition startPosition;
            BlockPosition endPosition;
            bool selectionMode;

            bool disposed;

            public WorldEditor(MultiplayerWorld world, PlayerSession playerSession)
            {
                this.World = world;
                this.PlayerSession = playerSession;
                this.selectionMode = false;
                this.disposed = false;

                playerSession.CommandParser.AddCommand(new BeginSelectBlocksCommandAction(this));
                playerSession.CommandParser.AddCommand(new DeselectCommandAction(this));
                playerSession.CommandParser.AddCommand(new FillCommandAction(this));

                playerSession.CommandParser.AddCommand(new LoadStructureCommandAction());
                playerSession.CommandParser.AddCommand(new SaveStructureCommandAction());
                playerSession.CommandParser.AddCommand(new GetStructureFromSelectionCommandAction(this));
                playerSession.CommandParser.AddCommand(new PlaceStructureCommandAction(this));

                if (playerSession.ExtensionManager.SupportsExtension("PlayerClick"))
                    playerSession.OnPlayerClick += this.OnPlayerClick;

                if (playerSession.ExtensionManager.SupportsExtension("TextHotKey"))
                {
                    playerSession.SendPacket(new SetTextHotkeyPacket("select", ".select\n", 31, SetTextHotkeyPacket.KeyModCtrl));
                    playerSession.SendPacket(new SetTextHotkeyPacket("deselect", ".deselect\n", 32, SetTextHotkeyPacket.KeyModCtrl));
                    playerSession.SendPacket(new SetTextHotkeyPacket("fill", ".fill\n", 35, SetTextHotkeyPacket.KeyModCtrl));
                    playerSession.Message("Please note these WorldEdit Shortcuts:\n"
                                        + " - CTRL+S to select.\n"
                                        + " - CTRL+D to deselect.\n"
                                        + " - CTRL+H to fill.", true);
                }
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
                QuitSelectionMode();
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
                {
                    if (currentSelection.Highlighted)
                        currentSelection.Unhilight();
                }
                currentSelection = null;
            }

            private void QuitSelectionMode()
            {
                if (selectionMode)
                {
                    if (startPosition != null)
                        DeselectStartPosition();
                    if (endPosition != null)
                        DeselectEndPosition();
                    selectionMode = false;
                }
            }

            private void DeselectStartPosition()
            {
                if (startPosition == null)
                    throw new InvalidOperationException();

                if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    PlayerSession.SendPacket(new RemoveSelectionPacket(1));
                else
                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(startPosition, World.GetBlock(startPosition)));
                startPosition = null;
            }

            private void SelectStartPosition()
            {
                if (startPosition != null)
                    DeselectStartPosition();

                startPosition = new BlockPosition(World.GetPlayerPosition(this.PlayerSession));
                if (!World.InWorld(startPosition))
                {
                    startPosition = null;
                    PlayerSession.Message("Please select a position inside the world.", false);
                    return;
                }

                PlayerSession.Message("You have selected block position 1: " + startPosition, false);

                if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    PlayerSession.SendPacket(new MakeSelectionPacket(1, "startSelection", startPosition, new BlockPosition(startPosition, 1, 1, 1), 144, 238, 144, 80));
                else
                {
                    PlayerSession.Message("Please select another position.", false);
                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(startPosition, Gameplay.Blocks.Green));
                }
            }

            private void DeselectEndPosition()
            {
                if (endPosition == null)
                    throw new InvalidOperationException();

                if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    PlayerSession.SendPacket(new RemoveSelectionPacket(2));
                else
                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(endPosition, World.GetBlock(endPosition)));
                endPosition = null;
            }

            private void SelectEndPosition()
            {
                if (endPosition != null)
                    DeselectEndPosition();

                endPosition = new BlockPosition(World.GetPlayerPosition(this.PlayerSession));
                if (!World.InWorld(endPosition))
                {
                    endPosition = null;
                    PlayerSession.Message("Please select a position inside the world.", false);
                    return;
                }

                PlayerSession.Message("You have selected block position 2: " + endPosition, false);

                if (PlayerSession.ExtensionManager.SupportsExtension("SelectionCuboid"))
                    PlayerSession.SendPacket(new MakeSelectionPacket(2, "endSelection", endPosition, new BlockPosition(endPosition, 1, 1, 1), 255, 99, 71, 80));
                else
                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(endPosition, Gameplay.Blocks.Red));
            }

            private void FinalizeSelectionProcess()
            {
                if (!this.selectionMode || (this.startPosition == null || this.endPosition == null))
                    throw new InvalidOperationException();
                Select(startPosition, endPosition);
            }

            public bool SetBlock(BlockPosition position, byte blockType)
            {
                if (selectionMode)
                {
                    PlayerSession.SendPacket(new Networking.Clientbound.SetBlockPacket(position, World.GetBlock(position)));

                    if (!PlayerSession.ExtensionManager.SupportsExtension("PlayerClick") && blockType != Gameplay.Blocks.Air)
                    {
                        if (startPosition == null)
                        {
                            SelectStartPosition();
                        }
                        else if (endPosition == null)
                        {
                            SelectEndPosition();
                            FinalizeSelectionProcess();
                        }
                    }
                    return false;
                }
                return true;
            }

            private void OnPlayerClick(object sender, PlayerClickedPacket playerClickedPacket)
            {
                if (selectionMode && playerClickedPacket.ClickAction == PlayerClickedPacket.Action.Released)
                {
                    if (playerClickedPacket.ClickButton == PlayerClickedPacket.Button.LeftClick)
                        SelectStartPosition();
                    else if (playerClickedPacket.ClickButton == PlayerClickedPacket.Button.RightClick)
                        SelectEndPosition();
                    else
                        FinalizeSelectionProcess();
                }
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
                        PlayerSession.CommandParser.RemoveCommand("deselect");
                        PlayerSession.CommandParser.RemoveCommand("fill");
                        PlayerSession.CommandParser.RemoveCommand("ldstruct");
                        PlayerSession.CommandParser.RemoveCommand("stostruct");
                        PlayerSession.CommandParser.RemoveCommand("cstruct");
                        PlayerSession.CommandParser.RemoveCommand("plstruct");
                        PlayerSession.OnPlayerClick = null;
                        Deselect();
                        QuitSelectionMode();
                    }
                }
            }
        }
    }
}
