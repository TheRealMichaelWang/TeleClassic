using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using TeleClassic.Networking;

namespace TeleClassic
{
    public sealed class Blacklist
    {
        public sealed class BanPlayerCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => false;

            public string GetName() => "ban";
            public string GetDescription() => "Permanantley bans a player.";

            Blacklist blacklist;

            public BanPlayerCommandAction(Blacklist blacklist)
            {
                this.blacklist = blacklist;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                if (commandProcessor.Permissions < Permission.Admin)
                    throw new ArgumentException("You need to be an admin to ban players.");
                CommandProcessor.StringCommandObject reasonObject = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                CommandProcessor.PlayerCommandObject playersToBan = (CommandProcessor.PlayerCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.PlayerCommandObject));
                foreach (PlayerSession playerSession in playersToBan.playerSessions)
                    blacklist.Ban(playerSession, reasonObject.String, DateTime.MaxValue);
            }
        }

        public sealed class TemporaryBanPlayerCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 3;
            public bool ReturnsValue() => false;

            public string GetName() => "tban";
            public string GetDescription() => "Temporarily bans a player.";

            Blacklist blacklist;

            public TemporaryBanPlayerCommandAction(Blacklist blacklist)
            {
                this.blacklist = blacklist;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                if (commandProcessor.Permissions < Permission.Admin)
                    throw new ArgumentException("You need to be an admin to ban players.");
                CommandProcessor.StringCommandObject reasonObject = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                CommandProcessor.StringCommandObject timeObject = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                CommandProcessor.PlayerCommandObject playersToBan = (CommandProcessor.PlayerCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.PlayerCommandObject));
                TimeSpan banSpan = TimeSpan.Parse(timeObject.String);
                foreach (PlayerSession playerSession in playersToBan.playerSessions)
                    blacklist.Ban(playerSession, reasonObject.String, DateTime.Now + banSpan);
            }
        }

        public sealed class KickPlayerCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 2;
            public bool ReturnsValue() => false;

            public string GetName() => "kick";
            public string GetDescription() => "Kicks a selection of players";

            Blacklist blacklist;

            public KickPlayerCommandAction(Blacklist blacklist)
            {
                this.blacklist = blacklist;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                if (commandProcessor.Permissions < Permission.Operator)
                    throw new ArgumentException("You need to be an op or admin to kick players.");
                CommandProcessor.StringCommandObject reasonObject = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                CommandProcessor.PlayerCommandObject playersToBan = (CommandProcessor.PlayerCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.PlayerCommandObject));
                foreach (PlayerSession playerSession in playersToBan.playerSessions)
                    playerSession.Kick(reasonObject.String);
            }
        }

        public struct IPBanEntry
        {
            public IPAddress Address;
            public DateTime BanExpiration;
            public string Reason;

            public IPBanEntry(IPAddress address, DateTime banExpiration, string reason)
            {
                this.Address = address;
                this.BanExpiration = banExpiration;
                this.Reason = reason;
            }

            public IPBanEntry(BinaryReader reader)
            {
                this.Address = new IPAddress(reader.ReadInt64());
                this.BanExpiration = new DateTime(reader.ReadInt64());
                this.Reason = reader.ReadString();
            }

            public void WriteBack(BinaryWriter writer)
            {
                writer.Write(Address.Address);
                writer.Write(BanExpiration.Ticks);
                writer.Write(Reason);
            }
        }

        public static BanPlayerCommandAction banPlayerCommandAction = new BanPlayerCommandAction(Program.blacklist);
        public static TemporaryBanPlayerCommandAction temporaryBanPlayerCommandAction = new TemporaryBanPlayerCommandAction(Program.blacklist);
        public static KickPlayerCommandAction kickPlayerCommandAction = new KickPlayerCommandAction(Program.blacklist);

        Dictionary<IPAddress, IPBanEntry> addressBanMap;
        string blackListFileDb;

        public Blacklist(string blackListFileDb)
        {
            this.blackListFileDb = blackListFileDb;
            if (File.Exists(blackListFileDb))
            {
                using (FileStream fileStream = new FileStream(blackListFileDb, FileMode.Open, FileAccess.Read))
                using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gZip))
                {
                    int ban_count = reader.ReadInt32();
                    addressBanMap = new Dictionary<IPAddress, IPBanEntry>(ban_count);
                    for(int i = 0; i < ban_count; i++)
                    {
                        IPBanEntry banEntry = new IPBanEntry(reader);
                        if (DateTime.Now >= banEntry.BanExpiration)
                        {
                            Logger.Log("Secuirty", "Ban expired. Issued because \""+banEntry.Reason+"\".", banEntry.Address.ToString());
                            addressBanMap.Add(banEntry.Address, banEntry);
                        }
                    }
                }
            }
            else {
                addressBanMap = new Dictionary<IPAddress, IPBanEntry>();
                File.Create(blackListFileDb).Close(); 
            }
        }

        public bool IsBanned(IPAddress address)
        {
            if (addressBanMap.ContainsKey(address))
            {
                if (DateTime.Now >= addressBanMap[address].BanExpiration)
                    return false;
                return true;
            }
            return false;
        }

        public IPBanEntry GetBanEntry(IPAddress address) => addressBanMap[address];

        public void Ban(PlayerSession playerSession, string reason, DateTime expiration)
        {
            if (playerSession.Permissions == Permission.Admin)
                throw new ArgumentException("Cannot ban an admin.");
            if (playerSession.Address == IPAddress.Loopback)
                throw new ArgumentException("Cannot ban localhost.");
            addressBanMap.Add(playerSession.Address, new IPBanEntry(playerSession.Address, expiration, reason));
            playerSession.Kick("You have just been banned: " + reason + ".");
        }

        public void Save()
        {
            using (FileStream fileStream = new FileStream(this.blackListFileDb, FileMode.Open, FileAccess.Write))
            using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Compress))
            using (BinaryWriter writer = new BinaryWriter(gZip))
            {
                writer.Write(addressBanMap.Count);
                foreach (IPBanEntry banEntry in addressBanMap.Values)
                    banEntry.WriteBack(writer);
            }
        }
    }
}
