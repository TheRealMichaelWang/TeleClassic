using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleClassic.gameplay.world;
using TeleClassic.networking.protocol;
using TeleClassic.networking.protocol.clientbound;

namespace TeleClassic.gameplay
{
    public static partial class Gameplay
    {
        public static Dictionary<string, MultiplayerWorld> Worlds;

        public static PlayerManager PlayerManager;

        public static bool IsActive;

        static Thread gameThread;

        static Queue<Tuple<TaskDelegate, object[]>> tasks;

        public static void Initialize()
        {
            makeDirExist("worlds");

            PlayerManager = new PlayerManager();
            tasks = new Queue<Tuple<TaskDelegate, object[]>>();
            Worlds = new Dictionary<string, MultiplayerWorld>();

            //load every classic world into memory
            foreach(FileInfo file in new DirectoryInfo("worlds").GetFiles())
            {
                if (file.Extension == ".cw")
                {
                    Console.WriteLine("Loading world from " + file.FullName + "...");
                    Worlds[file.Name] = new MultiplayerWorld(file.Name, World.FromFile(file.FullName), false);
                }
            }

            gameThread = new Thread(new ThreadStart(GameLoop));
        }

        public static void Begin()
        {
            IsActive = true;
            gameThread.Start();
        }

        public static void Stop()
        {
            Console.WriteLine("Stopping game thread...");
            IsActive = false;
            
            //give 5ms time to let game thread abort by itself
            Thread.Sleep(5);
            gameThread.Abort();

            while(gameThread.IsAlive)
            {
                //wait for the game thread to exit
            }
        }

        private static void GameLoop()
        {
            while(IsActive)
            {
                if(tasks.Count > 0)
                {
                    lock (tasks)
                    {
                        Tuple<TaskDelegate, object[]> task = tasks.Dequeue();
                        task.Item1.Invoke(task.Item2);
                    }
                }
            }
            Console.Write("Stopped, ("+tasks.Count+") task(s) remaining.");
        }

        //pushes a tasks to the servers task queue
        public static void ExecuteTask(TaskDelegate task, params object[] args)
        {
            lock (tasks)
            {
                tasks.Enqueue(new Tuple<TaskDelegate, object[]>(task, args));
            }
        }

        //crreates a directory if it doesn't exist
        private static void makeDirExist(string path)
        {
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    public partial class Player
    {
        //frees player resources
        public void Destroy()
        {
            Console.WriteLine(Username + "(ID:" + ID+ ") has left the server.");
            Gameplay.PlayerManager.RemovePlayer(ID);
            if (Location != null)
            {
                Gameplay.Worlds[Location.Identfier].LeaveWorld(this);
            }
        }
        
        //teleports a player to a location in their world
        public void Teleport(Position position)
        {
            this.Location.Position = position;
            ParentSession.SendPacket(new PositionAndOrientationPacket(-1, position.X, position.Y, position.Z, position.Yaw, position.Pitch));
            Gameplay.ExecuteTask(Gameplay.Worlds[Location.Identfier].UpdatePosition, this);
        }

        //sends a private message to a player
        public void Message(string message)
        {
            ParentSession.SendPacket(new MessagePacket(-1, message));
        }

        public void JoinWorld(object[] args) => JoinWorld((string)args[0]);
        
        //joins a new world. Be sure to call leave world before invoking
        public void JoinWorld(string worldName)
        {
            if(!Gameplay.Worlds.ContainsKey(worldName))
            {
                if(Gameplay.Worlds.ContainsKey(worldName + ".cw"))
                {
                    Gameplay.Worlds[worldName+".cw"].JoinWorld(this);
                    return;
                }
                else
                {
                    throw new InvalidOperationException("No such world \"" + worldName + "\" exists on our server.");
                }
            }
            Gameplay.Worlds[worldName].JoinWorld(this);
        }
    }
}
