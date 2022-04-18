using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TeleClassic;
using TeleClassic.Networking;

namespace TeleClassic
{
    public enum Permission
    {
        Member = 0,
        Operator = 1,
        Admin = 2
    }
    public sealed class Account
    {
        public sealed class RegisterAccountCommandAction : CommandProcessor.CommandAction
        {
            PlayerSession playerSession;
            AccountManager accountManager;
            string selectedUsername;
            string selectedPassword;

            public int GetExpectedArgumentCount() => selectedUsername == null ? 2 : 1;
            public bool ReturnsValue() => false;

            public string GetName() => "register";
            public string GetDescription() => "registers an account on the server.";

            public RegisterAccountCommandAction(AccountManager accountManager, PlayerSession playerSession)
            {
                this.accountManager = accountManager;
                this.playerSession = playerSession;
                this.selectedUsername = null;
                this.selectedPassword = null;
            }

            public RegisterAccountCommandAction(AccountManager accountManager, PlayerSession playerSession, string selectedUsername) : this(accountManager, playerSession)
            {
                if (accountManager.UserExists(selectedUsername))
                    throw new ArgumentException("Username has already been taken. Please select a unique username.");
                this.selectedUsername = selectedUsername;
            }
            
            public void Invoke(CommandProcessor commandProcessor)
            {
                if (this.selectedUsername == null) {
                    string username = ((CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject))).String;
                    if (accountManager.UserExists(username))
                        commandProcessor.Print("The username \"" + username + "\" has already been taken.");
                    else
                        this.selectedUsername = username;
                }
                CommandProcessor.StringCommandObject password = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                if(this.selectedPassword == null)
                {
                    this.selectedPassword = password.String;
                    commandProcessor.Print("To confirm your account registration:\nRetype \"/register [selected/already entered password]\".");
                }
                else
                {
                    if(this.selectedPassword != password.String)
                    {
                        commandProcessor.Print("Account registration succesfully canceled.");
                        this.selectedPassword = null;
                    }
                    else
                    {
                        try
                        {
                            this.playerSession.Account = accountManager.Register(this.selectedUsername, this.selectedPassword, Permission.Member);
                            commandProcessor.Print("Account registered succesfully.");
                            this.playerSession.CommandParser.RemoveCommand(this.GetName());
                        }
                        catch(ArgumentException e)
                        {
                            commandProcessor.Print("An error occured whilst registering your account:\n" + e.Message);
                            commandProcessor.Print("Account registration canceled.");
                            this.selectedUsername = null;
                            this.selectedPassword = null;
                        }
                    }
                }
            }
        }

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
                        if ((DateTime.Now - account.LastLogon).TotalDays >= 90)
                            Logger.Log("Info", "Deleted account after 90 days.", account.Username);
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
                throw new InvalidOperationException("Incorrect password - please wait at least 30 seconds to try again.");
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