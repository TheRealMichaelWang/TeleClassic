using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.gameplay;
using TeleClassic.networking;
using TeleClassic.networking.protocol;
using TeleClassic.networking.protocol.clientbound;
using TeleClassic.networking.protocol.serverbound;

namespace TeleClassic.networking
{
    public delegate void ProcessPacket(Packet packet);

    partial class Session
    {
        Player player;

        public void ProcessAllPackets()
        {
            Dictionary<byte, ProcessPacket> packetHandlers = new Dictionary<byte, ProcessPacket>()
            {
                {0, handlePlayerIdentificationPacket},
                {5, handleSetBlockPacket },
                {8, handlePlayerPositionUpdatePacket},
                {13, handleMessagePacket }
            };

            while(PacketsAvailible())
            {
                using (Packet toProc = WaitForPacket())
                {
                    try
                    {
                        packetHandlers[toProc.PacketID].Invoke(toProc);
                    }
                    catch(Exception e)
                    {
                        player.Message(ColorCode.Red + "[Error]: " + e.Message);
                    }
                }
            }
        }

        private void handleSetBlockPacket(Packet packet)
        {
            using(SetBlockPacket setBlockPacket = new SetBlockPacket(packet.ToByteArray()))
            {
                if(player.Location != null)
                {
                    if(Gameplay.Worlds[player.Location.Identfier].Locked)
                    {
                        SendPacket(new SetBlockPacket(setBlockPacket.X, setBlockPacket.Y, setBlockPacket.Z, Gameplay.Worlds[player.Location.Identfier][setBlockPacket.X, setBlockPacket.Y, setBlockPacket.Z]));
                        player.Message(ColorCode.Yellow + "[Warning]: You cannot edit a locked world.");
                        return;
                    }
                    if(setBlockPacket.Mode == BlockMode.Create)
                    {
                        Gameplay.Worlds[player.Location.Identfier][setBlockPacket.X, setBlockPacket.Y, setBlockPacket.Z] = setBlockPacket.BlockType;
                    }
                    else
                    {
                        Gameplay.Worlds[player.Location.Identfier][setBlockPacket.X, setBlockPacket.Y, setBlockPacket.Z] = Blocks.Air;
                    }
                    if (!Gameplay.Worlds[player.Location.Identfier].Locked)
                    {
                        Gameplay.ExecuteTask(Physics.Update, Gameplay.Worlds[player.Location.Identfier], setBlockPacket.X, setBlockPacket.Y, setBlockPacket.Z);
                    }
                }
            }
        }

        private void handlePlayerPositionUpdatePacket(Packet packet)
        {
            using (PositionAndOrientationPacket positionAndOrientation = new PositionAndOrientationPacket(packet.ToByteArray()))
            {
                if (player.Location != null)
                {
                    player.Location.Position.X = positionAndOrientation.X;
                    player.Location.Position.Y = positionAndOrientation.Y;
                    player.Location.Position.Z = positionAndOrientation.Z;
                    player.Location.Position.Yaw = positionAndOrientation.Yaw;
                    player.Location.Position.Pitch = positionAndOrientation.Pitch;
                    Gameplay.ExecuteTask(Gameplay.Worlds[player.Location.Identfier].UpdatePosition, player);
                }
            }
        }

        private void handlePlayerIdentificationPacket(Packet packet)
        {
            using (PlayerIdentficationPacket playerIdentfication = new PlayerIdentficationPacket(packet.ToByteArray()))
            {
                player = Gameplay.PlayerManager.CreatePlayer(playerIdentfication.Username, ServerInformation.DefaultUserType, this, null); //create new entity
                SendPacket(new ServerIdentificationPacket(ServerInformation.Version, ServerInformation.Name, ServerInformation.MessageOfTheDay, ServerInformation.DefaultUserType)); //respond with server information
                Gameplay.ExecuteTask(player.JoinWorld, "lobby");
            }
        }

        private void handleMessagePacket(Packet packet)
        {
            using(TeleClassic.networking.protocol.serverbound.MessagePacket messagePacket = new TeleClassic.networking.protocol.serverbound.MessagePacket(packet.ToByteArray()))
            {
                Gameplay.ExecuteTask(Gameplay.Worlds[player.Location.Identfier].Broadcast, messagePacket.Message);
            }
        }
    }
}
