using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;
using TeleClassic.Networking.Serverbound;
using TeleClassic;

namespace TeleClassic.Networking
{

    public sealed partial class PlayerSession : IDisposable
    {
        public sealed class PrintCommandAction : CommandProcessor.PrintCommandAction
        {
            PlayerSession playerSession;

            public PrintCommandAction(PlayerSession playerSession)
            {
                this.playerSession = playerSession;
            }

            public override void Print(string message) => playerSession.Message(message);
        }

        public sealed class GetCurrentPlayer : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => true;

            public string GetName() => "cp";
            public string GetDescription() => "Gets the current player(you).";

            PlayerSession playerSession;

            public GetCurrentPlayer(PlayerSession playerSession)
            {
                this.playerSession = playerSession;
            }

            public void Invoke(CommandProcessor commandProcessor) => commandProcessor.PushObject(new CommandProcessor.PlayerCommandObject(new List<PlayerSession>() { playerSession }));
        }

        public sealed class GetCurrentWorld : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => true;

            public string GetName() => "cw";
            public string GetDescription() => "Gets the current world you are in.";

            PlayerSession playerSession;

            public GetCurrentWorld(PlayerSession playerSession)
            {
                this.playerSession = playerSession;
            }

            public void Invoke(CommandProcessor commandProcessor) => commandProcessor.PushObject(new CommandProcessor.WorldCommandObject(new List<MultiplayerWorld>() { playerSession.currentWorld }));
        }

        public sealed class GotoWorld : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 1;
            public bool ReturnsValue() => false;

            public string GetName() => "go";
            public string GetDescription() => "(You) Go to another world.";

            PlayerSession playerSession;

            public GotoWorld(PlayerSession playerSession)
            {
                this.playerSession = playerSession;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                CommandProcessor.WorldCommandObject worldCommandObject = (CommandProcessor.WorldCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.WorldCommandObject));
                if (worldCommandObject.worlds.Count != 1)
                    throw new ArgumentException("Expected exactly 1 world to go to.");
                this.playerSession.JoinWorld(worldCommandObject.worlds[0]);
            }
        }

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

        Server server;
        volatile TcpClient client;
        volatile NetworkStream networkStream;
        volatile bool disposed;

        readonly Dictionary<byte, PacketHandler> packetHandlers;

        string guestName;
        Account account;
        MultiplayerWorld currentWorld;

        CommandParser commandParser;
        CommandProcessor commandProcessor;

        byte bufferedOpCode;
        volatile bool sendingPacket;

        public bool Disconnected { get; private set; }
        public bool HasPackets { get => Disconnected ? false : networkStream.DataAvailable; }
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
            this.client.LingerState.Enabled = true;
            this.client.LingerState.LingerTime = 30;
            networkStream = client.GetStream();
            disposed = false;
            Disconnected = false;
            bufferedOpCode = byte.MaxValue;
            this.IsMuted = false;
            this.sendingPacket = false;

            packetHandlers = new Dictionary<byte, PacketHandler>(){
                {0   , handlePlayerId},
                {0x08, handlePlayerUpdatePosition},
                {0x05, handlePlayerSetBlock},
                {0x0d, handlePlayerMessage}
            };
            commandParser = new CommandParser(new PrintCommandAction(this));
            commandParser.AddCommand(new GetCurrentPlayer(this));
            commandParser.AddCommand(new GetCurrentWorld(this));
            commandParser.AddCommand(new GotoWorld(this));
            Logger.Log("networking", "Accepted new client conncetion.", Address.ToString());
        }

        public bool SendPacket(Packet packet)
        {
            while (sendingPacket) { }

            bool stat;
            try
            {
                sendingPacket = true;
                packet.Send(networkStream);
                stat = true;
            }
            catch
            {
                stat = false;
            }
            sendingPacket = false;
            return stat;
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

        public void Message(string message)
        {
            message = message.Replace("\r",string.Empty);
            if(message.Contains("\n"))
            {
                foreach (string line in message.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    Message(line);
            }
            else
            {
                for (int i = 0; i < message.Length; i += 64)
                    SendPacket(new MessagePacket(-1, message.Substring(i, Math.Min(message.Length - i, 64))));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                if (disposing)
                {
                    Disconnected = true;
                    if (currentWorld != null)
                        currentWorld.LeaveWorld(this);
                    if (account != null)
                        server.AccountManager.Logout(account);
                    networkStream.Close();
                    client.Close();
                }
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
            commandProcessor = new CommandProcessor(this.Permissions, commandParser.printCommandAction);
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
            if (messagePacket.Message.StartsWith('.') || messagePacket.Message.StartsWith('/'))
            {
                string command = messagePacket.Message.TrimStart('.', '/');
                Message(command);
                try
                {
                    commandProcessor.ExecuteCommand(commandParser.Compile(command));
                }
                catch(ArgumentException e)
                {
                    Message(e.Message);
                }
            }
            else 
            {
                currentWorld.MessageFromPlayer(this, messagePacket.Message);
            }
        }
    }
}