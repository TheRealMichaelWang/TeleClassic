using System;
using System.Collections.Generic;

namespace TeleClassic.Networking.CEP
{
    public sealed class ExtensionManager
    {
        static List<ExtEntryPacket> ServerSupportedExtensions;
        static Dictionary<string, ExtEntryPacket> serverSupportedExtensionsIdMap;

        static ExtensionManager()
        {
            ServerSupportedExtensions = new List<ExtEntryPacket>();
            serverSupportedExtensionsIdMap = new Dictionary<string, ExtEntryPacket>();
        }

        public static void DeclareSupport(string extName, int version)
        {
            ExtEntryPacket extInfo = new ExtEntryPacket(extName, version);
            ServerSupportedExtensions.Add(extInfo);
            serverSupportedExtensionsIdMap.Add(extName, extInfo);
        }
        
        Dictionary<string, int> SupportedExtensions;
        PlayerSession playerSession;

        int clientSupportedExtensionCount;
        int recievedExtInfoPackets;

        public ExtensionManager(PlayerSession playerSession)
        {
            this.playerSession = playerSession;
            this.SupportedExtensions = new Dictionary<string, int>();
        }

        public void Negotiate()
        {
            playerSession.SendPacket(new ExtInfoPacket("TeleClassic", (short)ServerSupportedExtensions.Count));
            foreach (ExtEntryPacket serverSupportedExtension in ServerSupportedExtensions)
            {
                playerSession.SendPacket(serverSupportedExtension);
                this.SupportedExtensions[serverSupportedExtension.ExtName] = -1;
            }
            playerSession.AddPacketHandler(new PlayerSession.PacketHandler(0x10, 67, handleExtInfoPacket));
        }

        private void handleExtInfoPacket()
        {
            ExtInfoPacket extInfoPacket = new ExtInfoPacket(playerSession.networkStream);
            this.clientSupportedExtensionCount = extInfoPacket.ExtensionCount;
            this.recievedExtInfoPackets = 0;

            playerSession.AddPacketHandler(new PlayerSession.PacketHandler(0x11, 69, handleExtEntryPacket));
        }

        private void handleExtEntryPacket()
        {
            ExtEntryPacket extEntryPacket = new ExtEntryPacket(playerSession.networkStream);
            if (!this.SupportedExtensions.ContainsKey(extEntryPacket.ExtName))
                throw new InvalidOperationException("Unexpected extension \"" + extEntryPacket.ExtName + "\".");
            if (this.SupportedExtensions[extEntryPacket.ExtName] != -1)
                throw new InvalidOperationException("Already declared \"" + extEntryPacket.ExtName + "\".");

            this.SupportedExtensions[extEntryPacket.ExtName] = Math.Min(serverSupportedExtensionsIdMap[extEntryPacket.ExtName].Version, extEntryPacket.Version);
            this.recievedExtInfoPackets++;

            if(this.recievedExtInfoPackets == this.clientSupportedExtensionCount)
            {
                //finished negotiation
                playerSession.RemovePacketHandler(0x10);
                playerSession.RemovePacketHandler(0x11);
                playerSession.finalizeIdHandshake();
            }
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        IdentificationPacket playerId;

        public void finalizeIdHandshake()
        {
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
                if (Program.accountManager.UserExists(guestName))
                    this.CommandParser.AddCommand(new Account.RegisterAccountCommandAction(Program.accountManager, this));
                else
                    this.CommandParser.AddCommand(new Account.RegisterAccountCommandAction(Program.accountManager, this, guestName));
            }
            commandProcessor = new CommandProcessor(this.Permissions, CommandParser.printCommandAction);
            this.JoinWorld(Program.worldManager.Lobby);
        }

        public void handlePlayerId()
        {
            if (this.playerId != null)
                throw new InvalidOperationException("Already performed handshake.");

            this.playerId = new IdentificationPacket(networkStream);

            if(this.playerId.Permissions == 0x42)
            {
                extensionManager.Negotiate();
            }
            else
            {
                finalizeIdHandshake();
            }
        }
    }
}