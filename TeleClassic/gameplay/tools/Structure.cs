using System;
using System.IO;
using System.Text;
using TeleClassic.Gameplay;
using TeleClassic.Networking;

namespace TeleClassic.Gameplay
{
    public partial class PersonalWorld
    {
        public partial class WorldEditor
        {
            public sealed class LoadStructureCommandAction : CommandProcessor.CommandAction
            {
                public string GetName() => "ldstruct";
                public string GetDescription() => "Loads a saved structure.";

                public int GetExpectedArgumentCount() => 1;
                public bool ReturnsValue() => true;

                public void Invoke(CommandProcessor commandProcessor)
                {
                    CommandProcessor.StringCommandObject structureName = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));

                    if (!File.Exists("structures/" + structureName.String))
                        commandProcessor.Print("Unable to locate structure \"" + structureName.String + "\".");
                    else
                    {
                        using (FileStream fileStream = new FileStream("structures/" + structureName.String, FileMode.Open, FileAccess.Read))
                        using (BinaryReader reader = new BinaryReader(fileStream))
                            commandProcessor.PushObject(new StructureCommandObject(reader));
                    }
                }
            }

            public sealed class SaveStructureCommandAction : CommandProcessor.CommandAction
            {
                public string GetName() => "stostruct";
                public string GetDescription() => "Stores a saved structure.";

                public int GetExpectedArgumentCount() => 2;
                public bool ReturnsValue() => false;

                public void Invoke(CommandProcessor commandProcessor)
                {
                    CommandProcessor.StringCommandObject structureName = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                    StructureCommandObject structure = (StructureCommandObject)commandProcessor.PopObject(typeof(StructureCommandObject));

                    using (FileStream fileStream = new FileStream("structures/" + structureName.String, FileMode.OpenOrCreate, FileAccess.Write))
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                        structure.WriteBack(writer);
                }
            }

            public sealed class GetStructureFromSelectionCommandAction : CommandProcessor.CommandAction
            {
                public string GetName() => "cstruct";
                public string GetDescription() => "Gets the structure from your current selection.";

                public int GetExpectedArgumentCount() => 0;
                public bool ReturnsValue() => true;

                WorldEditor worldEditor;

                public GetStructureFromSelectionCommandAction(WorldEditor worldEditor)
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

                    commandProcessor.PushObject(new StructureCommandObject(worldEditor.currentSelection));
                    worldEditor.Deselect();
                }
            }

            public sealed class PlaceStructureCommandAction : CommandProcessor.CommandAction
            {
                public string GetName() => "plstruct";
                public string GetDescription() => "Places a structure at the players current position.";

                public int GetExpectedArgumentCount() => 1;
                public bool ReturnsValue() => false;

                WorldEditor worldEditor;

                public PlaceStructureCommandAction(WorldEditor worldEditor)
                {
                    this.worldEditor = worldEditor;
                }

                public void Invoke(CommandProcessor commandProcessor)
                {
                    StructureCommandObject structure = (StructureCommandObject)commandProcessor.PopObject(typeof(StructureCommandObject));
                    structure.Place(worldEditor.World, worldEditor.World.GetPlayerPosition(worldEditor.PlayerSession));
                }
            }

            public sealed class StructureCommandObject : CommandProcessor.CommandObject
            {
                private static short MagicNum = 8265;

                public readonly short XDim, YDim, ZDim;
                public readonly byte[,,] Blocks;

                public StructureCommandObject(BinaryReader binaryReader, bool check_magic_num = true)
                {
                    if (check_magic_num && binaryReader.ReadInt16() != MagicNum)
                        throw new ArgumentException("Invalid magic number.");

                    this.XDim = binaryReader.ReadInt16();
                    this.YDim = binaryReader.ReadInt16();
                    this.ZDim = binaryReader.ReadInt16();

                    this.Blocks = new byte[this.XDim, this.YDim, this.ZDim];
                    for (short x = 0; x < XDim; x++)
                        for (short y = 0; y < YDim; y++)
                            for (short z = 0; z < ZDim; z++)
                                this.Blocks[x, y, z] = binaryReader.ReadByte();
                }

                public StructureCommandObject(BlockSelection blockSelection)
                {
                    this.XDim = blockSelection.XDim;
                    this.YDim = blockSelection.YDim;
                    this.ZDim = blockSelection.ZDim;

                    this.Blocks = new byte[this.XDim, this.YDim, this.ZDim];
                    for (short x = 0; x < XDim; x++)
                        for (short y = 0; y < YDim; y++)
                            for (short z = 0; z < ZDim; z++)
                                this.Blocks[x, y, z] = blockSelection.World.GetBlock(new BlockPosition(x, y, z));
                }

                public void WriteBack(BinaryWriter binaryWriter, bool emit_magic_num = true)
                {
                    if (emit_magic_num)
                        binaryWriter.Write(MagicNum);

                    binaryWriter.Write(this.XDim);
                    binaryWriter.Write(this.YDim);
                    binaryWriter.Write(this.ZDim);

                    for (short x = 0; x < XDim; x++)
                        for (short y = 0; y < YDim; y++)
                            for (short z = 0; z < ZDim; z++)
                                binaryWriter.Write(this.Blocks[x, y, z]);
                }

                public void Place(MultiplayerWorld multiplayerWorld, BlockPosition position)
                {
                    multiplayerWorld.BeginBulkBlockUpdate();
                    for (short x = 0; x < XDim; x++)
                        for (short y = 0; y < YDim; y++)
                            for (short z = 0; z < ZDim; z++)
                                multiplayerWorld.SetBlock(new BlockPosition((short)(position.X + x), (short)(position.Y + y), (short)(position.Z + z)), Blocks[x,y,z]);
                    multiplayerWorld.FinalizeBulkBlockUpdate();
                }

                public void ToString(StringBuilder builder)
                {
                    builder.AppendLine((this.XDim * this.YDim * this.ZDim) + " block(s)");
                    builder.AppendLine(" - XDim: " + this.XDim);
                    builder.AppendLine(" - YDim: " + this.YDim);
                    builder.AppendLine(" - ZDim: " + this.ZDim);
                }
            }
        }
    }
}
