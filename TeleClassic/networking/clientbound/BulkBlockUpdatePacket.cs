using System;
using System.Collections.Generic;
using System.Net.Sockets;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class BulkBlockUpdatePacket : Packet
    {
        public struct BlockUpdate
        {
            public readonly int Index;
            public readonly BlockPosition Position;

            public readonly byte BlockType;

            public BlockUpdate(int index, BlockPosition position, byte blockType)
            {
                this.Index = index;
                this.Position = position;
                this.BlockType = blockType;
            }
        }

        public readonly byte Count;
        public readonly BlockUpdate[] BlockUpdates;

        public BulkBlockUpdatePacket(List<BlockUpdate> blockUpdates) : base(0x26)
        {
            if (blockUpdates.Count > 256 && blockUpdates.Count != 0)
                throw new InvalidOperationException("Cannot send bulk block update packet with " + blockUpdates.Count + " block updates.");
            this.BlockUpdates = blockUpdates.ToArray();
            this.Count = (byte)(blockUpdates.Count - 1);
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x26);
            writer.WriteByte(this.Count);

            foreach (BlockUpdate blockUpdate in this.BlockUpdates)
                writer.WriteInt(blockUpdate.Index);
            for (int i = this.BlockUpdates.Length; i < 256; i++)
                writer.WriteInt(0);

            foreach (BlockUpdate blockUpdate in this.BlockUpdates)
                writer.WriteByte(blockUpdate.BlockType);
            for (int i = this.BlockUpdates.Length; i < 256; i++)
                writer.WriteByte(0);
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class MultiplayerWorld
    {
        List<BulkBlockUpdatePacket.BlockUpdate> blockUpdates = new List<BulkBlockUpdatePacket.BlockUpdate>();
        volatile bool bulkBlockUpdateMode = false;

        public void BeginBulkBlockUpdate()
        {
            if (bulkBlockUpdateMode)
                throw new InvalidOperationException("Cannot initialize bulk block update while waiting to finalize.");
            bulkBlockUpdateMode = true;
        }

        public override void SetBlock(BlockPosition position, byte blockType)
        {
            if (GetBlock(position) == blockType)
                return;

            base.SetBlock(position, blockType);
            if (bulkBlockUpdateMode)
            {
                blockUpdates.Add(new BulkBlockUpdatePacket.BlockUpdate(this.IndexFromPosition(position), position, blockType));
            }
            else
            {
                foreach (PlayerSession player in playersInWorld)
                    player.SetBlock(position, blockType);
            }
        }

        public void FinalizeBulkBlockUpdate()
        {
            if (!bulkBlockUpdateMode)
                throw new InvalidOperationException("Cannot finalize bulk block updates because they haven't been started.");

            while (blockUpdates.Count > 0)
            {
                if (blockUpdates.Count > 160)
                {
                    List<BulkBlockUpdatePacket.BlockUpdate> toUpdate = blockUpdates.GetRange(0, Math.Min(256, blockUpdates.Count));
                    foreach (PlayerSession player in playersInWorld)
                    {
                        if(player.ExtensionManager.SupportsExtension("BulkBlockUpdate"))
                            player.SendPacket(new BulkBlockUpdatePacket(toUpdate));
                        else
                        {
                            foreach (BulkBlockUpdatePacket.BlockUpdate blockUpdate in toUpdate)
                                player.SetBlock(blockUpdate.Position, blockUpdate.BlockType);
                        }
                    }
                    blockUpdates.RemoveRange(0, toUpdate.Count);
                }
                else
                {
                    foreach(BulkBlockUpdatePacket.BlockUpdate blockUpdate in this.blockUpdates)
                        foreach(PlayerSession player in playersInWorld)                            
                            player.SetBlock(blockUpdate.Position, blockUpdate.BlockType);
                    this.blockUpdates.Clear();
                }
            }
            this.bulkBlockUpdateMode = false;
        }
    }
}