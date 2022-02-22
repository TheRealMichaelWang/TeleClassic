using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;
using TeleClassic.Networking.Serverbound;

namespace TeleClassic.Networking
{
    public sealed partial class PlayerSession : IDisposable
    {
        public delegate void PacketHandler();

        private Server server;
        private volatile TcpClient client;
        private volatile NetworkStream networkStream;
        private volatile bool disposed;

        private readonly Dictionary<byte, PacketHandler> packetHandlers;

        private string guestName;
        private Account account;
        private MultiplayerWorld currentWorld;

        public bool Disconnected { get; private set; }

        public string Name
        {
            get
            {
                if (account == null)
                    return guestName + "(guest)";
                else
                    switch (account.Permissions)
                    {
                        case Permission.Admin:
                            return account.Username + "(admin)";
                        case Permission.Operator:
                            return account.Username + "(op)";
                        default:
                            return account.Username;
                    }
            }
        }

        public Permission Permissions
        {
            get => account == null ? Permission.Member : account.Permissions;
        }

        public PlayerSession(TcpClient client, Server server)
        {
            this.server = server;
            this.client = client;
            networkStream = client.GetStream();
            disposed = false;
            Disconnected = false;

            packetHandlers = new Dictionary<byte, PacketHandler>(){
                {0   , handlePlayerId},
                {0x08, handlePlayerUpdatePosition},
                {0x05, handlePlayerSetBlock}
            };

            Logger.Log("networking", "Accepted new client conncetion.", client.Client.RemoteEndPoint.ToString());
        }

        public bool SendPacket(Packet packet)
        {
            try
            {
                packet.Send(networkStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Ping()
        {
            if (!SendPacket(new PingPacket()))
            {
                Disconnected = true;
                return false;
            }
            return true;
        }

        public void Kick(string reason)
        {
            SendPacket(new DisconnectPlayerPacket(reason));
            Thread.Sleep(100);
            Dispose();
        }

        public void Message(string message) => SendPacket(new MessagePacket(-1, message));

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (currentWorld != null)
                        currentWorld.LeaveWorld(this);
                    networkStream.Close();
                    client.Close();
                    Disconnected = true;
                }
                disposed = true;
            }
        }

        public void JoinWorld(MultiplayerWorld world)
        {
            if (currentWorld != null)
                LeaveWorld();
            world.JoinWorld(this);
            currentWorld = world;
        }

        public void LeaveWorld()
        {
            currentWorld.LeaveWorld(this);
            currentWorld = null;
        }

        public void ProcessAvailibleData()
        {
            if (Disconnected)
                return;

            while (networkStream.DataAvailable)
            {
                byte id = (byte)networkStream.ReadByte();
                if (!packetHandlers.ContainsKey(id))
                {
                    Logger.Log("error/networking", "Received invalid packet ID.", client.Client.RemoteEndPoint.ToString());
                    throw new InvalidOperationException("Your Client has a bug: Received invalid packet ID.");
                }
                packetHandlers[id]();
            }
        }

        public void handlePlayerId()
        {
            IdentificationPacket playerId = new IdentificationPacket(networkStream);

            if (playerId.ProtocolVersion != 0x07)
            {
                Logger.Log("error/networking", "Client used invalid protocol version.", client.Client.RemoteEndPoint.ToString());
                throw new InvalidOperationException("Invalid Protocol Version.");
            }

            try
            {
                account = server.accountManager.Login(playerId.Name, playerId.Key);
                SendPacket(new IdentificationPacket("TeleClassic", "You've sucesfully logged in.", 0x07, (account.Permissions >= Permission.Operator) ? (byte)0x64 : (byte)0x0));
            }
            catch (ArgumentException e)
            {
                guestName = playerId.Name;
                SendPacket(new IdentificationPacket("TeleClassic", e.Message, 0x07, 0x0));
            }
            this.JoinWorld(Program.Lobby);
        }

        public void handlePlayerUpdatePosition()
        {
            PositionAndOrientationPacket newPosition = new PositionAndOrientationPacket(networkStream);
            if(currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to update position but hasn't joined a world.", client.Client.RemoteEndPoint.ToString());
                throw new InvalidOperationException("Your Client has a bug: Updating position whilst not in world.");
            }
            currentWorld.UpdatePosition(this, newPosition.Position);
        }

        public void handlePlayerSetBlock()
        {
            Serverbound.SetBlockPacket setBlockPacket = new Serverbound.SetBlockPacket(networkStream);
            if (currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to set a block but hasn't joined a world.", client.Client.RemoteEndPoint.ToString());
                throw new InvalidOperationException("Your Client has a bug: setting blocks whilst not in world.");
            }
            if (setBlockPacket.Mode == 0x01) //create block
                currentWorld.SetBlock(this, setBlockPacket.Position, setBlockPacket.BlockType);
            else
                currentWorld.SetBlock(this, setBlockPacket.Position, Blocks.Air);
        }
    }
}