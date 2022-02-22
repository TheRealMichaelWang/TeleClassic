using System;
using TeleClassic;
using TeleClassic.Gameplay;
using TeleClassic.Networking;

class Program
{
    static Server server;
    static AccountManager accountManager;

    public static MultiplayerWorld Lobby;

    public static void Main(string[] args)
    {
        Lobby = new MultiplayerWorld("lobby.cw", Permission.Admin, Permission.Member, 127);

        Logger.Log("Info", "Begun starting.", "None");
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(exit);

        accountManager = new AccountManager("accounts.db");
        server = new Server(25565, accountManager);

        server.Start();
        Logger.Log("Info", "Finished starting.", "None");

        while (true)
        {
            string command = Console.ReadLine();
            if (command == "exit")
            {
                Environment.Exit(0);
            }
            else if(command == "logs")
            {
                Logger.PrintAll();
            }
        }
    }

    static void exit(object sender, EventArgs e)
    {
        Logger.Log("Info", "Begun stopping.", "None");
        server.Stop();
        accountManager.Save();

        Logger.Log("Info", "Finished stopping.", "None");

        Logger.EndSession();
    }
}