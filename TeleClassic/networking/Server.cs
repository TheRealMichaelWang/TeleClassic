using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TeleClassic;

namespace TeleClassic.Networking
{
    public sealed class Server
    {
        private readonly TcpListener listener;
        private readonly Thread serverThread;

        public readonly int Port;

        private readonly List<PlayerSession> sessions;
        public readonly AccountManager accountManager;

        private volatile bool exit;

        public Server(int port, AccountManager accountManager)
        {
            Port = port;
            sessions = new List<PlayerSession>();
            listener = new TcpListener(IPAddress.Any, port);

            serverThread = new Thread(new ThreadStart(serverLoop));

            exit = false;
            this.accountManager = accountManager;
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
                    sessions.Add(new PlayerSession(listener.AcceptTcpClient(), this));

                foreach (PlayerSession session in sessions)
                    if (!session.Disconnected && !session.Ping())
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
                            Logger.Log("networking/security", "Kicked player from server because of an invalid operation.", "None");
                        }
                    }
            }
            foreach (PlayerSession session in sessions)
                session.Kick("The server has been stopped, come back next time!");
        }
    }
}