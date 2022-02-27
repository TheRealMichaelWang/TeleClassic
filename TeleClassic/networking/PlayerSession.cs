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
        public static Dictionary<byte, int> expectedBytes = new Dictionary<byte, int>()
        {
            {0x00, 130}, //identification
            {0x05, 8}, //set block
            {0x08, 9}, //position and orientation
            {0x0d, 65}, //message
        };

        public delegate void PacketHandler();

        public bool IsMuted;
        public readonly IPAddress Address;

        private Server server;
        private volatile TcpClient client;
        private volatile NetworkStream networkStream;
        private volatile bool disposed;

        private readonly Dictionary<byte, PacketHandler> packetHandlers;

        private string guestName;
        private Account account;
        private MultiplayerWorld currentWorld;
        private byte bufferedOpCode;

        public bool Disconnected { get; private set; }
        public bool HasPackets { get => networkStream.DataAvailable; }
        public bool IsLoggedIn {get => account != null; }

        public Account Account
        {
            get
            {
                if (account == null)
                    throw new InvalidOperationException("User is not logged in.");
                return account;
            }
        }

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
            this.Address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            if (server.Blacklist.IsBanned(this.Address))
            {
                Blacklist.IPBanEntry banEntry = server.Blacklist.GetBanEntry(this.Address);
                Kick("You've been banned for: " + banEntry.Reason + ".");
                throw new InvalidOperationException("A banned player tried to connect.");
            }


            this.server = server;
            this.client = client;
            networkStream = client.GetStream();
            disposed = false;
            Disconnected = false;
            bufferedOpCode = byte.MaxValue;
            this.IsMuted = false;

            packetHandlers = new Dictionary<byte, PacketHandler>(){
                {0   , handlePlayerId},
                {0x08, handlePlayerUpdatePosition},
                {0x05, handlePlayerSetBlock},
                {0x0d, handlePlayerMessage}
            };

            Logger.Log("networking", "Accepted new client conncetion.", Address.ToString());
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
                    if (account != null)
                        server.AccountManager.Logout(account);
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

        public void HandleNextPacket()
        {
            if(bufferedOpCode == byte.MaxValue) //this is to prevent a potential partial packet DDOS attack
            {
                bufferedOpCode = (byte)networkStream.ReadByte();
                if (!packetHandlers.ContainsKey(bufferedOpCode))
                {
                    Logger.Log("error/networking", "Received invalid packet ID.", Address.ToString());
                    throw new InvalidOperationException("Your Client has a bug: Received invalid packet ID.");
                }
            }
            else if(client.Available >= expectedBytes[bufferedOpCode])
            {
                packetHandlers[bufferedOpCode]();
                bufferedOpCode = byte.MaxValue;
            }
        }

        public void handlePlayerId()
        {
            IdentificationPacket playerId = new IdentificationPacket(networkStream);

            if (playerId.ProtocolVersion != 0x07)
            {
                Logger.Log("error/networking", "Client used invalid protocol version.", Address.ToString());
                throw new InvalidOperationException("Invalid Protocol Version.");
            }

            try
            {
                account = server.AccountManager.Login(playerId.Name, playerId.Key);
                SendPacket(new IdentificationPacket("TeleClassic", "You've sucesfully logged in.", 0x07, (account.Permissions >= Permission.Operator) ? (byte)0x64 : (byte)0x0));
            }
            catch (ArgumentException e)
            {
                guestName = playerId.Name;
                SendPacket(new IdentificationPacket("TeleClassic", e.Message, 0x07, 0x0));
            }
            this.JoinWorld(Program.worldManager.Lobby);
        }

        public void handlePlayerUpdatePosition()
        {
            PositionAndOrientationPacket newPosition = new PositionAndOrientationPacket(networkStream);
            if(currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to update position but hasn't joined a world.", Address.ToString());
                throw new InvalidOperationException("Your Client has a bug: Updating position whilst not in world.");
            }
            currentWorld.UpdatePosition(this, newPosition.Position);
        }

        public void handlePlayerSetBlock()
        {
            Serverbound.SetBlockPacket setBlockPacket = new Serverbound.SetBlockPacket(networkStream);
            if (currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to set a block but hasn't joined a world.", Address.ToString());
                throw new InvalidOperationException("Your Client has a bug: setting blocks whilst not in world.");
            }
            if (setBlockPacket.Mode == 0x01) //create block
                currentWorld.SetBlock(this, setBlockPacket.Position, setBlockPacket.BlockType);
            else
                currentWorld.SetBlock(this, setBlockPacket.Position, Blocks.Air);
        }

        public void handlePlayerMessage()
        {
            MessagePacket messagePacket = new MessagePacket(networkStream);
            if (currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to send a message but hasn't joined a world.", Address.ToString());
                throw new InvalidOperationException("Your Client has a bug: setting blocks whilst not in world.");
            }
            if (messagePacket.Message.StartsWith('.'))
            {

            }
            else 
            {
                currentWorld.MessageFromPlayer(this, messagePacket.Message);
            }
        }
    }
}