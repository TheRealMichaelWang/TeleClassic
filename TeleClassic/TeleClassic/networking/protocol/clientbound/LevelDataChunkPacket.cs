using System;

namespace TeleClassic.networking.protocol.clientbound
{
    public class LevelDataChunkPacket:Packet
    {
        public readonly short ChunkLength;
        public readonly byte[] ChunkData;
        public readonly byte PercentComplete;

        public LevelDataChunkPacket(short chunkLength, byte[] chunkData, byte percentComplete) : base(3)
        {
            this.ChunkLength = chunkLength;
            this.ChunkData = chunkData;
            this.PercentComplete = percentComplete;
            WriteShort(chunkLength);
            WriteByteArray(chunkData);
            WriteByte(percentComplete);
        }
    }
}
