using System;
using System.Collections.Generic;
using System.IO;
using TeleClassic;
using TeleClassic.Gameplay;
using TeleClassic.Networking;
using TeleClassic.Networking.CEP;

class Program
{
    class ConsolePrintCommandAction : CommandProcessor.PrintCommandAction {
        public override void Print(string message) => Console.WriteLine(message);
    }

    public static Server server;
    public static AccountManager accountManager;
    public static WorldManager worldManager;
    public static Blacklist blacklist;

    static ConsolePrintCommandAction ConsolePrintCommand = new ConsolePrintCommandAction();

    public static void Main(string[] args)
    {
        ProtocolExtensionManager.DeclareSupport("PlayerClick", 1);
        ProtocolExtensionManager.DeclareSupport("SelectionCuboid", 1);
        ProtocolExtensionManager.DeclareSupport("TextHotKey", 1);
        ProtocolExtensionManager.DeclareSupport("MessageTypes", 1);
        ProtocolExtensionManager.DeclareSupport("HeldBlock", 1);
        ProtocolExtensionManager.DeclareSupport("CustomBlocks", 1);

        Logger.Log("Info", "Begun loading worlds.", "None");

        Logger.Log("Info", "Begun starting.", "None");
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(exit);

        accountManager = new AccountManager("accounts.db");
        blacklist = new Blacklist("blacklist.db");
        server = new Server(25565, accountManager, blacklist);
        worldManager = new WorldManager(new MultiplayerWorld("fuck.cw", Permission.Admin, Permission.Member, MultiplayerWorld.MaxPlayerCapacity), accountManager, "worlds.db");

        CommandProcessor commandProcessor = new CommandProcessor(Permission.Admin, ConsolePrintCommand);
        CommandParser commandParser = new CommandParser(ConsolePrintCommand);
        
        server.Start();
        Logger.Log("Info", "Finished starting.", "None");

        if (!accountManager.UserExists("michaelw"))
            accountManager.Logout(accountManager.Register("michaelw", "iamgod", Permission.Admin));

        //worldManager.AddPersonalWorld(new WorldManager.PersonalWorld("fuck.cw", null, true));

        while (true)
        {
            Console.Write(">");
            string command = Console.ReadLine();
            if (command == "exit")
            {
                Environment.Exit(0);
            }
            else if(command == "logs")
            {
                Logger.PrintAll();
            }
            else
            {
                try
                {
                    commandProcessor.ExecuteCommand(commandParser.Compile(command));
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }
    }

    static void exit(object sender, EventArgs e)
    {
        Logger.Log("Info", "Begun stopping.", "None");
        server.Stop();
        accountManager.Save();
        worldManager.Save();
        blacklist.Save();
        Logger.Log("Info", "Finished stopping.", "None");

        Logger.EndSession();
    }
}