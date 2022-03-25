using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Serverbound;

namespace TeleClassic.Networking.Serverbound
{
    public sealed class PlayerClickedPacket : Packet
    {
        public enum Button
        {
            LeftClick = 0,
            RightClick = 1,
            MiddleClick = 2
        }

        public enum Action
        {
            Pressed = 0,
            Released = 1
        }

        public readonly Button ClickButton;
        public readonly Action ClickAction;

        public readonly short Yaw;
        public readonly short Pitch;

        public readonly byte TargetEntityID;

        public readonly BlockPosition TargetedBlock;
        public readonly byte TargetedBlockFace;

        public PlayerClickedPacket(NetworkStream networkStream) : base(0x22)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(networkStream);
            this.ClickButton = (Button)reader.ReadByte();
            this.ClickAction = (Action)reader.ReadByte();
            this.Yaw = reader.ReadShort();
            this.Pitch = reader.ReadShort();
            this.TargetEntityID = reader.ReadByte();
            this.TargetedBlock = new BlockPosition(reader);
            this.TargetedBlockFace = reader.ReadByte();
        }

        public override void Send(NetworkStream stream) => new InvalidOperationException();
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        public EventHandler<PlayerClickedPacket> OnPlayerClick
        {
            get => playerClickedEvent;
            set
            {
                if (value != null && !ExtensionManager.SupportsExtension("PlayerClick"))
                    throw new InvalidOperationException("Unsupported protocol extension player clicked.");
                playerClickedEvent = value;
            }
        }

        EventHandler<PlayerClickedPacket> playerClickedEvent;

        public void handlePlayerClick()
        {
            PlayerClickedPacket playerClickedPacket = new PlayerClickedPacket(networkStream);
            if (playerClickedEvent != null)
                playerClickedEvent(this, playerClickedPacket);
        }
    }
}