using System;
using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking
{
    public sealed class PositionAndOrientationPacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly PlayerPosition Position;

        public PositionAndOrientationPacket(sbyte playerID, PlayerPosition position) : base(0x08)
        {
            PlayerID = playerID;
            Position = position;
        }

        public PositionAndOrientationPacket(NetworkStream stream) : base(0x08)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            PlayerID = reader.ReadSByte();
            Position = new PlayerPosition(reader);
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x08);
            writer.WriteSByte(PlayerID);
            Position.WriteBack(writer);
        }
    }

    public partial class PlayerSession
    {
        public byte HeldBlock
        {
            get
            {
                if (currentWorld == null || !ExtensionManager.SupportsExtension("HeldBlock"))
                    throw new InvalidOperationException();
                return lastHeldBlock;
            }
        }

        byte lastHeldBlock;

        public void handlePlayerUpdatePosition()
        {
            PositionAndOrientationPacket newPosition = new PositionAndOrientationPacket(networkStream);
            if (currentWorld == null)
            {
                if (joiningWorld)
                    return;
                Logger.Log("error/networking", "Client tried to update position but hasn't joined a world.", Address.ToString());
                throw new InvalidOperationException("Your Client has a bug: Updating position whilst not in world.");
            }
            lastHeldBlock = (byte)newPosition.PlayerID;
            currentWorld.UpdatePosition(this, newPosition.Position);
        }
    }
}