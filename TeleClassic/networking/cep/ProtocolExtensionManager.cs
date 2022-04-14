using System;
using System.Collections.Generic;

namespace TeleClassic.Networking.CEP
{
    public sealed class ProtocolExtensionManager
    {
        static List<ExtEntryPacket> ServerSupportedExtensions;
        static Dictionary<string, ExtEntryPacket> serverSupportedExtensionsIdMap;

        static ProtocolExtensionManager()
        {
            ServerSupportedExtensions = new List<ExtEntryPacket>();
            serverSupportedExtensionsIdMap = new Dictionary<string, ExtEntryPacket>();
        }

        public static bool Supports(string extName) => serverSupportedExtensionsIdMap.ContainsKey(extName);

        public static int GetMaxSupportedVersion(string extName)
        {
            if (!Supports(extName))
                throw new InvalidOperationException("No such supported extension \"" + extName + "\"");
            else
                return serverSupportedExtensionsIdMap[extName].Version;
        }

        public static void DeclareSupport(string extName, int version)
        {
            ExtEntryPacket extInfo = new ExtEntryPacket(extName, version);
            ServerSupportedExtensions.Add(extInfo);
            serverSupportedExtensionsIdMap.Add(extName, extInfo);
        }

        public byte CustomBlockSupportLevel
        {
            get
            {
                if (!SupportsExtension("CustomBlocks"))
                    throw new InvalidOperationException();
                return this.customBlockSupportLevel;
            }
        }

        Dictionary<string, int> SupportedExtensions;
        PlayerSession playerSession;

        int clientSupportedExtensionCount;
        int recievedExtInfoPackets;
        byte customBlockSupportLevel;

        public bool SupportsExtension(string extensionName) => SupportedExtensions.ContainsKey(extensionName) && SupportedExtensions[extensionName] != -1;

        public int GetExtensionVersion(string extensionName)
        {
            if (!SupportsExtension(extensionName))
                throw new InvalidOperationException("Unsupported extension.");
            return SupportedExtensions[extensionName];
        }

        public ProtocolExtensionManager(PlayerSession playerSession)
        {
            this.playerSession = playerSession;
            this.SupportedExtensions = new Dictionary<string, int>();
            this.customBlockSupportLevel = Gameplay.ExtendedBlocks.MaxSupportLevel;
        }

        public void Negotiate()
        {
            playerSession.SendPacket(new ExtInfoPacket("TeleClassic", (short)ServerSupportedExtensions.Count));
            foreach (ExtEntryPacket serverSupportedExtension in ServerSupportedExtensions)
            {
                playerSession.SendPacket(serverSupportedExtension);
                this.SupportedExtensions[serverSupportedExtension.ExtName] = -1;
            }
            playerSession.AddPacketHandler(new PlayerSession.PacketHandler(0x10, 66, handleExtInfoPacket));
        }

        public void NegotiateCustomBlocks()
        {
            playerSession.SendPacket(new CustomBlockSupportLevelPacket(this.customBlockSupportLevel));
            playerSession.AddPacketHandler(new PlayerSession.PacketHandler(0x13, 1, handleCustomBlockPacket));
        }

        private void handleCustomBlockPacket()
        {
            CustomBlockSupportLevelPacket customBlockSupportLevelPacket = new CustomBlockSupportLevelPacket(playerSession.networkStream);
            this.customBlockSupportLevel = Math.Min(this.customBlockSupportLevel, customBlockSupportLevelPacket.SupportLevel);

            playerSession.RemovePacketHandler(0x13);
            playerSession.finalizeIdHandshake();
        }

        private void handleExtInfoPacket()
        {
            ExtInfoPacket extInfoPacket = new ExtInfoPacket(playerSession.networkStream);
            this.clientSupportedExtensionCount = extInfoPacket.ExtensionCount;
            this.recievedExtInfoPackets = 0;

            playerSession.AddPacketHandler(new PlayerSession.PacketHandler(0x11, 68, handleExtEntryPacket));
        }

        private void handleExtEntryPacket()
        {
            ExtEntryPacket extEntryPacket = new ExtEntryPacket(playerSession.networkStream);
            this.recievedExtInfoPackets++;

            if(this.recievedExtInfoPackets == this.clientSupportedExtensionCount)
            {
                //finished negotiation
                playerSession.RemovePacketHandler(0x10);
                playerSession.RemovePacketHandler(0x11);

                if (SupportsExtension("CustomBlocks"))
                    NegotiateCustomBlocks();
                else
                    playerSession.finalizeIdHandshake();
            }

            if (!this.SupportedExtensions.ContainsKey(extEntryPacket.ExtName))
                return;
            if (this.SupportedExtensions[extEntryPacket.ExtName] != -1)
                throw new InvalidOperationException("Already declared \"" + extEntryPacket.ExtName + "\".");

            this.SupportedExtensions[extEntryPacket.ExtName] = Math.Min(serverSupportedExtensionsIdMap[extEntryPacket.ExtName].Version, extEntryPacket.Version);
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        IdentificationPacket playerId;

        public void handlePlayerId()
        {
            if (this.playerId != null)
                throw new InvalidOperationException("Already performed handshake.");

            this.playerId = new IdentificationPacket(networkStream);

            if(this.playerId.Permissions == 0x42)
            {
                ExtensionManager.Negotiate();
            }
            else
            {
                finalizeIdHandshake();
            }
        }
    }
}