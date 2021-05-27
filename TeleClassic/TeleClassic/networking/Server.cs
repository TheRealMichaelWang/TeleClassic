using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeleClassic.networking
{
    public partial class Server
    {
        TcpListener listener;
        Thread serverThread;
        List<Session> sessions;
        bool exit;

        public Server()
        {
            sessions = new List<Session>();
            listener = new TcpListener(IPAddress.Any, 80);
            serverThread = new Thread(new ThreadStart(serverLoop));
            exit = false;
        }

        public void Start()
        {
            listener.Start();
            serverThread.Start();
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server...");
            listener.Stop();
            exit = true;
            Thread.Sleep(5);
            serverThread.Abort();
            while(serverThread.IsAlive)
            {
                //wait for the thread to exit;
            }
        }

        private bool PendingConnections()
        {
            try
            {
                return listener.Pending();
            }
            catch
            {
                return false;
            }
        }

        private void serverLoop()
        {
            List<Session> closedSessions = new List<Session>();
            while (!exit)
            {
                //check for new connections
                while(PendingConnections())
                {
                    sessions.Add(new Session(listener.AcceptTcpClient()));
                }

                //check for closed sessions
                foreach(Session session in sessions)
                {
                    if(!session.SendPing())
                    {
                        closedSessions.Add(session);
                    }
                }
                foreach(Session session in closedSessions)
                {
                    sessions.Remove(session);
                }
                closedSessions.Clear();

                //process packets
                foreach(Session session in sessions)
                {
                    if (session.PacketsAvailible())
                    {
                        //process all availible packets
                        try
                        {
                            session.ProcessAllPackets();
                        }
                        catch
                        {
                            if(!session.SendPing())
                            {
                                closedSessions.Add(session);
                            }
                        }
                    }
                }
            }
        }
    }
}
