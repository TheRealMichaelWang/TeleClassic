using System;
using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class LevelDataChunkPacket : Packet
    {
        public readonly short ChunkLength;
        public readonly byte[] ChunkData;
        public byte PercentComplete;

        public LevelDataChunkPacket(short chunkLength, byte[] chunk, byte percentComplete) : base(0x03)
        {
            if (chunk.Length > 1024)
                throw new InvalidOperationException("Cannot send chunk larger than 1024 blocks.");
            ChunkLength = chunkLength;
            ChunkData = chunk;
            PercentComplete = percentComplete;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x03);
            writer.WriteShort(ChunkLength);
            for (int i = 0; i < ChunkLength; i++)
                writer.WriteByte(ChunkData[i]);
            for (int i = ChunkLength; i < 1024; i++)
                writer.WriteByte(0);
            writer.WriteByte(PercentComplete);
        }
    }
}