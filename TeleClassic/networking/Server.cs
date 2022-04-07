using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TeleClassic.Networking
{
    public sealed class Server
    {
        public sealed class GetAllPlayersCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => true;

            public string GetName() => "ps";
            public string GetDescription() => "Gets a list of all players in the server.";

            Server server;

            public GetAllPlayersCommandAction(Server server)
            {
                this.server = server;
            }

            public void Invoke(CommandProcessor commandProcessor) => commandProcessor.PushObject(new CommandProcessor.PlayerCommandObject(new List<PlayerSession>(server.sessions)));
        }

        public int PlayerCount { get => sessions.Count; }

        public static GetAllPlayersCommandAction getAllPlayersCommandAction = new GetAllPlayersCommandAction(Program.server);

        private readonly TcpListener listener;
        private readonly Thread serverThread;

        public readonly int Port;

        private readonly List<PlayerSession> sessions;
        public readonly AccountManager AccountManager;
        public readonly Blacklist Blacklist;

        private volatile bool exit;

        public Server(int port, AccountManager accountManager, Blacklist blacklist)
        {
            Port = port;
            sessions = new List<PlayerSession>();
            listener = new TcpListener(IPAddress.Any, port);

            serverThread = new Thread(new ThreadStart(serverLoop));

            exit = false;
            this.AccountManager = accountManager;
            this.Blacklist = blacklist;
        }

        public void Start()
        {
            listener.Start();
            serverThread.Start();
            Logger.Log("networking/info", "Started listening on port " + Port + ".", "None");
        }

        public void Stop()
        {
            Logger.Log("networking/info", "Stopping server...", "None");
            exit = true;

            Logger.Log("networking/info", "Awaiting server thread exit.", "None");
            while (serverThread.IsAlive) { } //wait for thread to exit

            listener.Stop();
            Logger.Log("networking/info", "Server and all threads stopped.", "None");
        }

        private void serverLoop()
        {
            Queue<PlayerSession> closedSessions = new Queue<PlayerSession>();
            while (!exit)
            {
                while (listener.Pending())
                    try
                    {
                        sessions.Add(new PlayerSession(listener.AcceptTcpClient(), this));
                    }
                    catch (InvalidOperationException e)
                    {
                        Logger.Log("info/networking", "Unable to connect client: " + e.Message + ".", "None");
                    }

                foreach (PlayerSession session in sessions)
                    if (session.Disconnected || !session.Ping())
                        closedSessions.Enqueue(session);

                while (closedSessions.Count > 0)
                {
                    PlayerSession toClose = closedSessions.Dequeue();
                    toClose.Dispose();
                    sessions.Remove(toClose);
                }

                foreach (PlayerSession session in sessions)
                    if (session.HasPackets)
                    {
                        try
                        {
                            session.HandleNextPacket();
                        }
                        catch (InvalidOperationException e)
                        {
                            session.Kick(e.Message);
                            closedSessions.Enqueue(session);
                            Logger.Log("networking/security", "Kicked player from server because of an invalid operation.", session.Name);
                        }
                    }
            }
            foreach (PlayerSession session in sessions)
                session.Kick("The server has been stopped, come back next time!");
        }
    }
}