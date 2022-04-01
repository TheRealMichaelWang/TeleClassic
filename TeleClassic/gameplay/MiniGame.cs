using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SuperForth;
using TeleClassic.Networking;

namespace TeleClassic.Gameplay
{
    public sealed class MiniGameMarshaller
    {
        public sealed class AddMinigameCommandAction : CommandProcessor.CommandAction
        {
            public string GetName() => "ldmgame";
            public string GetDescription() => "Loads a minigame config if the game succesfully loads.";

            public int GetExpectedArgumentCount() => 0;
            public bool ReturnsValue() => false;

            MiniGameMarshaller gameMarshaller;

            public AddMinigameCommandAction(MiniGameMarshaller gameMarshaller)
            {
                this.gameMarshaller = gameMarshaller;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                Console.WriteLine("***Add a Minigame Configuration***");
                Console.WriteLine("Please enter the script file. (ends with .sf, .txt, or .bin)");
                Console.Write(">");
                string scriptPath = Console.ReadLine();
                Console.WriteLine("Please enter the world file. (ends with .cw usually, or has no extension)");
                Console.Write(">");
                string worldPath = Console.ReadLine();

                gameMarshaller.AddNewMiniGame(new MiniGameConfiguration(scriptPath, worldPath, MultiplayerWorld.MaxPlayerCapacity, Permission.Member, Permission.Member));
            }
        }

        public sealed class MiniGameConfiguration
        {
            public readonly string ScriptFile;
            public readonly string WorldFile;

            public readonly int PlayerCapacity;
            public readonly Permission MinimumJoinPerms;
            public readonly Permission MinimumBuildPerms;

            public int FaliureCount;
            public int PlayCount;
            public readonly DateTime CreateDate;

            public bool Suspended;

            public bool ShouldSuspend() => this.FaliureCount > 3 && PlayCount < 100;

            public MiniGameConfiguration(string scriptFile, string worldFile, int playerCapacity, Permission minimumJoinPerms, Permission minimumBuildPerms)
            {
                this.ScriptFile = scriptFile;
                this.WorldFile = worldFile;
                this.PlayerCapacity = playerCapacity;
                this.MinimumJoinPerms = minimumJoinPerms;
                this.MinimumBuildPerms = minimumBuildPerms;
                this.FaliureCount = 0;
                this.PlayCount = 0;
                this.CreateDate = DateTime.Now;
                this.Suspended = false;
            }

            public MiniGameConfiguration(BinaryReader reader)
            {
                this.ScriptFile = reader.ReadString();
                this.WorldFile = reader.ReadString();
                this.PlayerCapacity = reader.ReadInt32();
                this.MinimumJoinPerms = (Permission)reader.ReadByte();
                this.MinimumBuildPerms = (Permission)reader.ReadByte();
                this.FaliureCount = reader.ReadInt32();
                this.PlayCount = reader.ReadInt32();
                this.CreateDate = new DateTime(reader.ReadInt64());
                this.Suspended = reader.ReadBoolean();
            }

            public MiniGame LoadMinigame(MiniGameMarshaller miniGameMarshaller)
            {
                if (this.Suspended)
                    throw new InvalidOperationException("Cannot start a minigame that has been suspended.");
                Logger.Log("Info", "Loading new minigame instance under marshall.", "None");
                MiniGame miniGame = new MiniGame(this, miniGameMarshaller.handleMinigameError, miniGameMarshaller.handleMinigameExit);
                miniGameMarshaller.miniGames.Add(miniGame);
                miniGameMarshaller.miniGameConfigurationMaps.Add(miniGame, this);
                return miniGame;
            }

            public void ResolveSuspsension()
            {
                this.Suspended = false;
                this.FaliureCount = 0;
            }

            public void WriteBack(BinaryWriter writer)
            {
                writer.Write(this.ScriptFile);
                writer.Write(this.WorldFile);
                writer.Write(this.PlayerCapacity);
                writer.Write((byte)this.MinimumJoinPerms);
                writer.Write((byte)this.MinimumBuildPerms);
                writer.Write(this.FaliureCount);
                writer.Write(this.PlayCount);
                writer.Write(this.CreateDate.Ticks);
                writer.Write(this.Suspended);
            }
        }

        List<MiniGame> miniGames;
        List<MiniGameConfiguration> miniGameConfigurations;
        Dictionary<MiniGame, MiniGameConfiguration> miniGameConfigurationMaps;

        private bool stopping;

        public MiniGameMarshaller()
        {
            stopping = false;
            if (File.Exists("minigames.db"))
            {
                using(FileStream stream = new FileStream("minigames.db", FileMode.Open, FileAccess.Read)) 
                using(BinaryReader reader = new BinaryReader(stream))
                {
                    int size = reader.ReadInt32();
                    miniGames = new List<MiniGame>(size);
                    miniGameConfigurations = new List<MiniGameConfiguration>(size);
                    miniGameConfigurationMaps = new Dictionary<MiniGame, MiniGameConfiguration>(size);

                    for (int i = 0; i < size; i++)
                        miniGameConfigurations.Add(new MiniGameConfiguration(reader));

                    foreach (MiniGameConfiguration miniGameConfiguration in miniGameConfigurations)
                        miniGameConfiguration.LoadMinigame(this);

                    foreach (MiniGame miniGame in miniGames)
                        miniGame.Start();
                }
            }
            else
            {
                File.Create("minigames.db").Close();
                miniGames = new List<MiniGame>();
                miniGameConfigurations = new List<MiniGameConfiguration>();
                miniGameConfigurationMaps = new Dictionary<MiniGame, MiniGameConfiguration>();
            }
        }

        public bool AddNewMiniGame(MiniGameConfiguration miniGameConfiguration)
        {
            try
            {
                MiniGame miniGame = miniGameConfiguration.LoadMinigame(this);
                this.miniGameConfigurations.Add(miniGameConfiguration);
                miniGame.Start();
                return true;
            }
            catch(SuperForthException exception)
            {
                Logger.Log("error/minigames", "Could not add minigame because it failed to load: "+exception.Message, "None.");
                return false;
            }
        }
        
        public void Stop()
        {
            stopping = true;
            foreach (MiniGame miniGame in this.miniGames)
                miniGame.Stop();
        }

        public void Save()
        {
            using(FileStream fileStream = new FileStream("minigames.db", FileMode.Open, FileAccess.Write))
            using(BinaryWriter writer = new BinaryWriter(fileStream))
            {
                writer.Write(miniGameConfigurations.Count);
                foreach (MiniGameConfiguration miniGameConfiguration in miniGameConfigurations)
                    miniGameConfiguration.WriteBack(writer);
            }
        }

        private void handleMinigameError(object sender, SuperForthException error)
        {
            MiniGame miniGame = (MiniGame)sender;
            Logger.Log("Info", "A minigame runtime error occured: " + error.Message, miniGame.Name);
            miniGames.Remove(miniGame);

            miniGameConfigurationMaps[miniGame].FaliureCount++;
            if(miniGameConfigurationMaps[miniGame].ShouldSuspend())
            {
                Logger.Log("Info", "A minigame has crashed to many times. It will be suspended until it is fixed.", miniGame.Name);
                miniGameConfigurationMaps[miniGame].Suspended = true;
            }
            else
            {
                Logger.Log("Info", "Restarting minigame after faliure.", miniGame.Name);
                miniGameConfigurationMaps[miniGame].LoadMinigame(this).Start();
            }
            miniGameConfigurationMaps.Remove(miniGame);
            miniGame.Dispose();
        }

        private void handleMinigameExit(object sender, bool succesful)
        {
            MiniGame miniGame = (MiniGame)sender;
            if (succesful)
            {
                Logger.Log("Info", "Minigame sucesfully finished after an uptime of: " + miniGame.UpTime, miniGame.Name);
                if (!stopping)
                {
                    miniGames.Remove(miniGame);
                    if (miniGame.UpTime < TimeSpan.FromMinutes(2))
                    {
                        Logger.Log("Info", "Suspending minigame because uptime is to short (<2mins).", miniGame.Name);
                        miniGameConfigurationMaps[miniGame].Suspended = true;
                    }
                }
                miniGameConfigurationMaps.Remove(miniGame);
                miniGame.Dispose();
            }
        }
    }

    public sealed partial class MiniGame : MultiplayerWorld, IDisposable
    {
        private struct MiniGameRuntimeConfiguration
        {
            public string Name;
            public string Description;
            public Account Author;
            public string WorkingDirectory;

            public MiniGameRuntimeConfiguration(SuperForthInstance.MachineHeapAllocation superforthConfigStruct)
            {
                this.Name = superforthConfigStruct[0].HeapAllocation.GetString();
                this.Description = superforthConfigStruct[1].HeapAllocation.GetString();

                this.WorkingDirectory = Environment.CurrentDirectory + "\\" + superforthConfigStruct[3].HeapAllocation.GetString();
                if (!Directory.Exists(this.WorkingDirectory))
                    throw new ArgumentException("Requested working directory does not exist.");

                string authorName = superforthConfigStruct[2].HeapAllocation.GetString();
                if (!Program.accountManager.UserExists(authorName))
                    throw new ArgumentException("Author does not exist on server.");
                this.Author = Program.accountManager.FindUser(authorName);
            }
        }

        public new string Name
        {
            get => configuration.Name;
        }

        public string Description
        {
            get => configuration.Description;
        }

        public Account Author
        {
            get => configuration.Author;
        }

        public TimeSpan UpTime
        {
            get => DateTime.Now - startTime;
        }

        MiniGameRuntimeConfiguration configuration;
        private volatile bool configured;

        DateTime startTime;
        SuperForthInstance gameInstance;
        Thread gameThread;
        private volatile bool exited;

        EventHandler<SuperForthException> errorHandler;
        EventHandler<bool> exitHandler;
        private bool disposed;

        public MiniGame(MiniGameMarshaller.MiniGameConfiguration configuration, EventHandler<SuperForthException> errorHandler, EventHandler<bool> exitHandler) : base(configuration.WorldFile, configuration.MinimumBuildPerms, configuration.MinimumJoinPerms, configuration.PlayerCapacity)
        {
            this.errorHandler = errorHandler;
            this.exitHandler = exitHandler;
            gameInstance = new SuperForthInstance(configuration.ScriptFile);
            Logger.Log("Info/Minigames", "Loaded game instance.", "None");
            gameInstance.AddForeignFunction(this.FFIConfigureMinigame);
            gameInstance.AddForeignFunction(this.FFILogInfo);
            gameThread = new Thread(new ThreadStart(gameThreadLoop));
            this.configured = false;
        }

        ~MiniGame()
        {
            this.Dispose(true);
        }

        public void Start()
        {
            this.exited = false;
            gameThread.Start();

            DateTime beginTime = DateTime.Now;
            while (!this.configured)
            {
                if(DateTime.Now - beginTime > TimeSpan.FromSeconds(3))
                {
                    this.Stop();
                    throw new InvalidOperationException("SuperForth Script did not finish configuration during alloted time-frame.");
                }
            }
        }

        public void Stop()
        {
            if (!this.exited)
            {
                Logger.Log("Info", "Stopping minigame.", this.Name);
                gameInstance.Pause();
                while(gameThread.IsAlive) { }
                gameInstance.Dispose();
                this.exited = true;
            }
        }

        private int FFIConfigureMinigame(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (configured)
                return 0;
            try
            {
                configuration = new MiniGameRuntimeConfiguration(input.HeapAllocation);

                configured = true;
                Program.worldManager.AddWorld(this);

                Logger.Log("Info", "Configuration finished, moving on to regular execution.", this.Name);
                return 1;
            }
            catch
            {
                return 0;
            }
        }

        private int FFILogInfo(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            Logger.Log("minigame/logs", input.HeapAllocation.GetString(), this.Name);
            return 1;
        }

        private void gameThreadLoop()
        {
            this.startTime = DateTime.Now;
            try
            {
                gameInstance.Run();
                this.exited = true;
                Logger.Log("Info", "The minigame has finished and exited willfully.", this.Name);
                exitHandler.Invoke(this, true);
            }
            catch(SuperForthException error)
            {
                Logger.Log("Info", "A runtime error has occured while running a minigame: " + error.Message, this.Name);
                errorHandler.Invoke(this, error);
                exitHandler.Invoke(this, false);
            }
            this.exited = true;
        }

        public void Dispose() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            Stop();
            if (disposing)
            {
                this.gameInstance.Dispose();
            }
        }
    }
}
