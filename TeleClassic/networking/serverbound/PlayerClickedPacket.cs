using SuperForth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.Gameplay;
using TeleClassic.Networking;
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

namespace TeleClassic.Gameplay
{
    public partial class MiniGame
    {
        private void handlePlayerClick(object sender, PlayerClickedPacket playerClickedPacket)
        {
            if (this.configuration.ExcludeBlockClicks && !BlockPosition.IsInvalid(playerClickedPacket.TargetedBlock))
                return;

            PlayerSession playerSession = (PlayerSession)sender;

            if (this.configuration.RunTagQueue && playerClickedPacket.TargetEntityID == 255)
            {
                this.gameInstance.Pause();
                PlayerSession taggedPlayer = idPlayerMap[(sbyte)playerClickedPacket.TargetEntityID];

                SuperForthInstance.MachineRegister eventRegister = SuperForthInstance.MachineRegister.NewHeapAlloc(this.gameInstance, 2, SuperForthInstance.MachineHeapAllocation.GCTraceMode.None);
                SuperForthInstance.MachineHeapAllocation playerTaggedInfo = eventRegister.HeapAllocation;
                playerTaggedInfo[0] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[taggedPlayer]);
                playerTaggedInfo[1] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]);
                
                eventRegister.GCKeepAlive(this.gameInstance);
                eventIds.Enqueue(MinigameEventID.PlayerTagged);
                eventArguments.Enqueue(Tuple.Create(eventRegister, true));
                this.gameInstance.ThreadResume();
            }
            else if(this.configuration.RunClickQueue)
            {
                this.gameInstance.Pause();
                SuperForthInstance.MachineRegister eventRegister = SuperForthInstance.MachineRegister.NewHeapAlloc(this.gameInstance, 5, SuperForthInstance.MachineHeapAllocation.GCTraceMode.None);
                SuperForthInstance.MachineHeapAllocation playerClickInfo = eventRegister.HeapAllocation;
                playerClickInfo[0] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]);
                playerClickInfo[1] = SuperForthInstance.MachineRegister.FromInt((int)playerClickedPacket.ClickButton);
                playerClickInfo[2] = SuperForthInstance.MachineRegister.FromBool(playerClickedPacket.ClickAction == PlayerClickedPacket.Action.Pressed);
                playerClickInfo[3] = SuperForthInstance.MachineRegister.FromInt(playerClickedPacket.Yaw);
                playerClickInfo[4] = SuperForthInstance.MachineRegister.FromInt(playerClickedPacket.Pitch);

                eventRegister.GCKeepAlive(this.gameInstance);
                eventIds.Enqueue(MinigameEventID.PlayerClick);
                eventArguments.Enqueue(Tuple.Create(eventRegister, true));
                this.gameInstance.ThreadResume(); 
            }
        }
    }
}