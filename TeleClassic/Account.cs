using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TeleClassic
{
    public enum Permission
    {
        Member,
        Operator,
        Admin
    }

    public sealed class Account
    {
        public readonly string Username;
        public readonly string Password;

        public DateTime LastLogon;
        public Permission Permissions;

        public bool IsLoggedIn;

        public Account(string username, string password, DateTime lastLogon, Permission permissions)
        {
            Username = username;
            Password = password;
            LastLogon = lastLogon;
            Permissions = permissions;
        }

        public Account(BinaryReader reader)
        {
            Username = reader.ReadString();
            Password = reader.ReadString();
            LastLogon = new DateTime(reader.ReadInt64());
            Permissions = (Permission)reader.ReadByte();
        }

        public void WriteBack(BinaryWriter writer)
        {
            writer.Write(Username);
            writer.Write(Password);
            writer.Write(LastLogon.Ticks);
            writer.Write((byte)Permissions);
        }
    }

    public sealed class AccountManager
    {
        private readonly Dictionary<string, Account> usernameLookup;
        private readonly List<Account> allAccounts;

        private readonly string accountDbFile;

        public bool UserExists(string username) => usernameLookup.ContainsKey(username);
        public Account FindUser(string username) => usernameLookup[username];

        public AccountManager(string accountDbFile)
        {
            Logger.Log("Info", "Loading accounts...", "None");
            this.accountDbFile = accountDbFile;

            usernameLookup = new Dictionary<string, Account>();
            allAccounts = new List<Account>();

            if (File.Exists(accountDbFile))
            {
                using (FileStream fileStream = new FileStream(accountDbFile, FileMode.Open, FileAccess.Read))
                using (GZipStream gzip = new GZipStream(fileStream, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gzip))
                {
                    int account_count = reader.ReadInt32();
                    for (int i = 0; i < account_count; i++)
                    {
                        Account account = new Account(reader);
                        if ((DateTime.Now - account.LastLogon).TotalDays >= 30)
                            Logger.Log("Info", "Deleted account after 30 days.", account.Username);
                        else
                        {
                            allAccounts.Add(account);
                            usernameLookup[account.Username] = account;
                        }
                    }
                }
            }
            else
                File.Create(accountDbFile).Close();
        }

        public void Save()
        {
            using (FileStream fileStream = new FileStream(accountDbFile, FileMode.Open, FileAccess.Write))
            using (GZipStream gzip = new GZipStream(fileStream, CompressionMode.Compress))
            using (BinaryWriter writer = new BinaryWriter(gzip))
            {
                writer.Write(allAccounts.Count);
                foreach (Account account in allAccounts)
                    account.WriteBack(writer);
            }
            Logger.Log("Info", "Saved accounts.", "None");
        }

        public Account Login(string username, string password)
        {
            if (!usernameLookup.ContainsKey(username))
                throw new ArgumentException("No such username exists.", "username");
            Account toAuth = usernameLookup[username];
            if (toAuth.Password != password)
            {
                Logger.Log("Security", "Incorrect password submitted.", username);
                throw new ArgumentException("Incorrect password - please wait at least 30 seconds to try again.", "password");
            }
            if (toAuth.IsLoggedIn)
            {
                Logger.Log("Security", "Succesful login attempt while logged in.", username);
                throw new InvalidOperationException("User already logged in.");
            }
            toAuth.IsLoggedIn = true;
            toAuth.LastLogon = DateTime.Now;
            Logger.Log("Info", "User succesfully logged in.", username);
            return toAuth;
        }

        public Account Register(string username, string password, Permission permissions)
        {
            if (usernameLookup.ContainsKey(username))
                throw new ArgumentException("Username has already been taken.", "username");
            if (username.Length > 12)
                throw new ArgumentException("Username is to long (must be under 12 chars)", "username");
            if (password.Length < 5)
                throw new ArgumentException("Password length is too short(must be longer than 5 characters).", "password");
            Account newAccount = new Account(username, password, DateTime.Now, permissions);
            newAccount.IsLoggedIn = true;
            usernameLookup.Add(username, newAccount);
            allAccounts.Add(newAccount);
            Logger.Log("Info", "New account registered.", username);
            return newAccount;
        }

        public void Logout(Account loggedInAccount)
        {
            if (!loggedInAccount.IsLoggedIn)
                throw new InvalidOperationException("Cannot log user out that has already been logged in.");
            loggedInAccount.IsLoggedIn = false;
        }
    }
}