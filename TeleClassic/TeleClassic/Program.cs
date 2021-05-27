using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.gameplay.world;
using TeleClassic.networking;
using TeleClassic.gameplay;

namespace TeleClassic
{
    class Program
    {
        static Server server;

        static void Main(string[] args)
        {
            Console.Title = "TeleClassic";
            Console.WriteLine("TeleClassic version 1\nBy Michael Wang\n");
            Console.CancelKeyPress += SaveAndExit;

            server = new Server();
            Gameplay.Initialize();

            Console.WriteLine("Starting gameplay...");
            Gameplay.Begin();
            Console.WriteLine("Starting server...");
            server.Start();

            Console.WriteLine("\nCommensed listening on port 80... Press CTRL+C to exit and save.\n");
        }

        private static void SaveAndExit(object sender, ConsoleCancelEventArgs e)
        {
            server.Stop();
            Gameplay.Stop();
        }
    }
}
