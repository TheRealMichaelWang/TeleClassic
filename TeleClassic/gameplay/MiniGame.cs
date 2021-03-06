using SuperForth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TeleClassic.Networking;
using TeleClassic.Networking.Clientbound;

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

                if (!gameMarshaller.AddNewMiniGame(new MiniGameConfiguration(scriptPath, worldPath, int.MaxValue, Permission.Member, Permission.Member)))
                    Console.WriteLine("Error: Minigame couldn't be added.");
            }
        }

        public sealed class UnsuspendMinigameCommandAction : CommandProcessor.CommandAction
        {
            public string GetName() => "unsusmgame";
            public string GetDescription() => "Unsuspends a minigame";

            public int GetExpectedArgumentCount() => 1;
            public bool ReturnsValue() => false;

            MiniGameMarshaller gameMarshaller;

            public UnsuspendMinigameCommandAction(MiniGameMarshaller gameMarshaller)
            {
                this.gameMarshaller = gameMarshaller;
            }

            public void Invoke(CommandProcessor commandProcessor)
            {
                CommandProcessor.StringCommandObject minigameName = (CommandProcessor.StringCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.StringCommandObject));
                if (!gameMarshaller.idMinigameConfigurationMap.ContainsKey(minigameName.String))
                    commandProcessor.Print("Could not find minigame \"" + minigameName.String + "\". Please enter the script file.");
                else if (commandProcessor.Permissions < Permission.Admin)
                    commandProcessor.Print("Your permissions are not sufficient enough to unsuspend minigames.");
                else
                {
                    gameMarshaller.idMinigameConfigurationMap[minigameName.String].ResolveSuspsension();
                }
            }
        }

        public static UnsuspendMinigameCommandAction unsuspendMinigameCommandAction = new UnsuspendMinigameCommandAction(Program.miniGameMarshaller);

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
                try
                {
                    MiniGame miniGame = new MiniGame(this, miniGameMarshaller.handleMinigameError, miniGameMarshaller.handleMinigameExit);
                    miniGameMarshaller.miniGames.Add(miniGame);
                    miniGameMarshaller.miniGameConfigurationMap.Add(miniGame, this);
                    return miniGame;
                }
                catch (SuperForthException e)
                {
                    Logger.Log("Error", "Could not load minigame: " + e.Message, "None");
                    return null;
                }
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

        public int ActiveMinigames { get => miniGames.Count; }

        List<MiniGame> miniGames;
        List<MiniGameConfiguration> miniGameConfigurations;
        Dictionary<string, MiniGameConfiguration> idMinigameConfigurationMap;
        Dictionary<MiniGame, MiniGameConfiguration> miniGameConfigurationMap;

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
                    miniGameConfigurationMap = new Dictionary<MiniGame, MiniGameConfiguration>(size);
                    idMinigameConfigurationMap = new Dictionary<string, MiniGameConfiguration>(size);

                    for (int i = 0; i < size; i++)
                    {
                        MiniGameConfiguration config = new MiniGameConfiguration(reader);
                        miniGameConfigurations.Add(config);
                        idMinigameConfigurationMap.Add(config.ScriptFile, config);
                    }

                    foreach (MiniGameConfiguration miniGameConfiguration in miniGameConfigurations)
                        if (!miniGameConfiguration.Suspended && miniGameConfiguration.LoadMinigame(this) == null)
                        {
                            Logger.Log("Info/Security", "Suspended minigame for being unable to load.", "None");
                            miniGameConfiguration.Suspended = true;
                        }

                    List<MiniGame> miniGamesToStart = new List<MiniGame>(miniGames);
                    foreach (MiniGame miniGame in miniGamesToStart)
                        try
                        {
                            miniGame.Start();
                        }
                        catch(InvalidOperationException)
                        {
                            Logger.Log("error-minigame", "Minigame failed to configure within 3 sec threshold.", "None");
                        }
                }
            }
            else
            {
                File.Create("minigames.db").Close();
                miniGames = new List<MiniGame>();
                miniGameConfigurations = new List<MiniGameConfiguration>();
                miniGameConfigurationMap = new Dictionary<MiniGame, MiniGameConfiguration>();
            }
        }

        public bool AddNewMiniGame(MiniGameConfiguration miniGameConfiguration)
        {
            MiniGame miniGame = miniGameConfiguration.LoadMinigame(this);
            if(miniGame == null)
            {
                Logger.Log("Info", "Could not add minigame during runtime.", "None");
                return false;
            }
            this.miniGameConfigurations.Add(miniGameConfiguration);
            miniGame.Start();
            return true;
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
            miniGames.Remove(miniGame);

            miniGameConfigurationMap[miniGame].FaliureCount++;
            if(miniGameConfigurationMap[miniGame].ShouldSuspend())
            {
                Logger.Log("Info", "A minigame has crashed to many times. It will be suspended until it is fixed.", miniGame.Name);
                miniGameConfigurationMap[miniGame].Suspended = true;
            }
            else
            {
                Logger.Log("Info", "Restarting minigame after faliure.", miniGame.Name);
                miniGameConfigurationMap[miniGame].LoadMinigame(this).Start();
            }
            miniGameConfigurationMap.Remove(miniGame);
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
                        miniGameConfigurationMap[miniGame].Suspended = true;
                    }
                }
                miniGameConfigurationMap.Remove(miniGame);
                miniGame.Dispose();
            }
        }
    }

    public sealed partial class MiniGame : MultiplayerWorld, IDisposable
    {
        private struct MiniGameRuntimeConfiguration
        {
            public readonly string Name;
            public readonly string Description;
            public readonly Account Author;
            public readonly string WorkingDirectory;

            public bool RunExitQueue;
            public bool RunMessageQueue;
            public bool RunClickQueue;
            public bool RunBlockQueue;
            public bool RunMoveQueue;
            public bool RunTagQueue;

            public bool ExcludeBlockClicks;
            public bool ExcludeTeleportMove;
            public bool DeferBlockPlace;

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

                this.RunExitQueue = superforthConfigStruct[4].BoolFlag;
                this.RunMessageQueue = superforthConfigStruct[5].BoolFlag;
                this.RunClickQueue = superforthConfigStruct[6].BoolFlag;
                this.RunBlockQueue = superforthConfigStruct[7].BoolFlag;
                this.RunMoveQueue = superforthConfigStruct[8].BoolFlag;
                this.RunTagQueue = superforthConfigStruct[9].BoolFlag;

                this.ExcludeBlockClicks = superforthConfigStruct[10].BoolFlag;
                this.ExcludeTeleportMove = superforthConfigStruct[11].BoolFlag;
                this.DeferBlockPlace = superforthConfigStruct[12].BoolFlag;
            }
        }

        private enum MinigameEventID
        {
            NoneOrEmpty = -1,
            PlayerExit = 0,
            PlayerMessage = 1,
            PlayerClick = 2,
            BlockPlaced = 3,
            PlayerMove = 4,
            PlayerTagged = 5
        }

        public new string Name
        {
            get => configured ? configuration.Name : "Unconfigured minigame";
        }

        public string Description
        {
            get => configured ? configuration.Description : "Unconfigured minigame";
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
        private volatile bool exited_thread;
        private volatile bool stopping;
        private volatile bool cleaned_users;

        EventHandler<SuperForthException> errorHandler;
        EventHandler<bool> exitHandler;
        private bool disposed;

        Queue<PlayerSession> joinQueue;
        HashSet<PlayerSession> inJoinQueue;
        HashSet<PlayerSession> teleportingPlayers;
        Queue<MinigameEventID> eventIds;
        Queue<Tuple<SuperForthInstance.MachineRegister, bool>> eventArguments; 

        Dictionary<PlayerSession, long> playerHandles;
        Dictionary<long, PlayerSession> handlePlayerMap;
        Queue<long> unusedHandles;
        long currentHandle;

        private volatile bool initialized = false;

        SuperForthInstance.Machine.ForeignFunction configureMiniGameDelegate;
        SuperForthInstance.Machine.ForeignFunction logInfoDelegate;
        SuperForthInstance.Machine.ForeignFunction getQueueTopDelegate;
        SuperForthInstance.Machine.ForeignFunction acceptPlayerDelegate;
        SuperForthInstance.Machine.ForeignFunction rejectPlayerDelegate;
        SuperForthInstance.Machine.ForeignFunction getEventID;
        SuperForthInstance.Machine.ForeignFunction getEventArg;
        SuperForthInstance.Machine.ForeignFunction getNextEvent;
        SuperForthInstance.Machine.ForeignFunction setActorPlayer;
        PlayerSession actorPlayer;

        public MiniGame(MiniGameMarshaller.MiniGameConfiguration configuration, EventHandler<SuperForthException> errorHandler, EventHandler<bool> exitHandler) : base(configuration.WorldFile, configuration.MinimumBuildPerms, configuration.MinimumJoinPerms, configuration.PlayerCapacity)
        {
            this.errorHandler = errorHandler;
            this.exitHandler = exitHandler;

            try
            {
                gameInstance = new SuperForthInstance(configuration.ScriptFile);
            }
            catch(SuperForthException e)
            {
                this.disposed = true;
                throw new SuperForthException(e.Error);
            }

            joinQueue = new Queue<PlayerSession>();
            inJoinQueue = new HashSet<PlayerSession>();
            eventIds = new Queue<MinigameEventID>();
            eventArguments = new Queue<Tuple<SuperForthInstance.MachineRegister, bool>>();
            playerHandles = new Dictionary<PlayerSession, long>();
            handlePlayerMap = new Dictionary<long, PlayerSession>();
            teleportingPlayers = new HashSet<PlayerSession>();
            unusedHandles = new Queue<long>();

            Logger.Log("Info/Minigames", "Loaded game instance.", "None");
            gameInstance.AddForeignFunction(configureMiniGameDelegate = this.FFIConfigureMinigame);
            gameInstance.AddForeignFunction(logInfoDelegate = this.FFILogInfo);
            gameInstance.AddForeignFunction(getQueueTopDelegate = this.FFIGetQueueTop);
            gameInstance.AddForeignFunction(acceptPlayerDelegate = this.FFIAcceptPlayer);
            gameInstance.AddForeignFunction(rejectPlayerDelegate = this.FFIRejectPlayer);
            gameInstance.AddForeignFunction(getEventID = this.FFIGetEventID);
            gameInstance.AddForeignFunction(getEventArg = this.FFIGetEventArg);
            gameInstance.AddForeignFunction(getNextEvent = this.FFINextEvent);
            gameInstance.AddForeignFunction(setActorPlayer = this.FFISetActorPlayer);
            gameInstance.AddForeignFunction(sendUrgentMessageDelegate = this.FFISendUrgentMessage);
            gameInstance.AddForeignFunction(sendBackloggedMessageDelegate = this.FFISendBackloggedMessage);
            gameInstance.AddForeignFunction(playerAnnounceDelegate = this.FFIPlayerAnnounce);
            gameInstance.AddForeignFunction(setPlayerStatusMessagePositionDelegate = this.FFISetPlayerStatusMessagePosition);
            gameInstance.AddForeignFunction(setPlayerStatusMessageDelegate = this.FFISetPlayerStatusMessage);
            gameInstance.AddForeignFunction(getPlayerInfoDelegate = this.FFIGetPlayerInfo);
            gameInstance.AddForeignFunction(getPlayerPositionDelegate = this.FFIGetPlayerPosition);
            gameInstance.AddForeignFunction(teleportPlayerDelegate = this.FFITeleportPlayer);

            initialized = true;
            gameThread = new Thread(new ThreadStart(gameThreadLoop));
        }

        ~MiniGame()
        {
            this.Dispose(true);
        }

        public void Start()
        {
            while (!initialized) { }

            this.configured = false;
            this.currentHandle = 0;
            this.exited_thread = false;
            this.cleaned_users = false;
            this.stopping = false;
            playerHandles.Clear();
            handlePlayerMap.Clear();
            unusedHandles.Clear();
            joinQueue.Clear();
            inJoinQueue.Clear();
            gameThread.Start();

            DateTime beginTime = DateTime.Now;
            while (!this.configured)
            {
                if(DateTime.Now - beginTime > TimeSpan.FromSeconds(3))
                {
                    this.Stop();
                    throw new InvalidOperationException("SuperForth Script did not finish configuration during allotted time-frame.");
                }
            }
        }

        public void Stop()
        {
            if (!this.exited_thread)
            {
                Logger.Log("Info", "Stopping minigame.", this.Name);
                this.stopping = true;
                gameInstance.Pause();
                while(gameThread.IsAlive) { }
                this.exited_thread = true;
            }
            if (!this.cleaned_users)
            {
                foreach (PlayerSession playerSession in this.joinQueue)
                    if (!playerSession.Disconnected)
                        playerSession.Kick("Minigame has been stopped.");
                this.cleaned_users = true;
            }
        }

        public override void TeleportPlayer(PlayerSession playerSession, PlayerPosition playerPosition)
        {
            teleportingPlayers.Add(playerSession);
            base.TeleportPlayer(playerSession, playerPosition);
        }

        public override void JoinWorld(PlayerSession playerSession)
        {
            Logger.Log("minigame/info", "Player has entered the join queue.", playerSession.Name);
            if (joinQueue.Count > 0)
            {
                playerSession.Message("You have entered the join queue for: " + this.Name + "\n" +
                                        "You are no. " + joinQueue.Count + " on the list.", false);
            }
            if(playerSession.ExtensionManager.SupportsExtension("MessageTypes"))
                playerSession.SendPacket(new MessagePacket(1, "Joining " + this.Name + "..."));

            joinQueue.Enqueue(playerSession);
            inJoinQueue.Add(playerSession);
        }

        public override void LeaveWorld(PlayerSession playerSession)
        {
            if (playerHandles.ContainsKey(playerSession))
            {
                if (configuration.RunExitQueue)
                {
                    eventIds.Enqueue(MinigameEventID.PlayerExit);
                    eventArguments.Enqueue(Tuple.Create(SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]), false));
                }

                if (!inJoinQueue.Contains(playerSession))
                {
                    base.LeaveWorld(playerSession);
                    if (this.configuration.RunClickQueue)
                        playerSession.OnPlayerClick += null;
                    FreeHandle(playerSession);
                }
            }
            playerSession.ClearPersistantMessages();
        }

        public override void UpdatePosition(PlayerSession playerSession, PlayerPosition newPosition)
        {
            if (!inJoinQueue.Contains(playerSession))
            {
                if (this.GetPlayerPosition(playerSession).Equals(newPosition))
                {
                    if (teleportingPlayers.Contains(playerSession))
                    {
                        teleportingPlayers.Remove(playerSession);
                    }
                    else if (this.configuration.RunMoveQueue)
                    {
                        this.gameInstance.Pause();

                        SuperForthInstance.MachineRegister eventRegister = SuperForthInstance.MachineRegister.NewHeapAlloc(this.gameInstance, 6, SuperForthInstance.MachineHeapAllocation.GCTraceMode.None);
                        SuperForthInstance.MachineHeapAllocation playerMoveInfo = eventRegister.HeapAllocation;
                        newPosition.SuperForthSerialize(playerMoveInfo);
                        playerMoveInfo[5] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]);
                        eventRegister.GCKeepAlive(this.gameInstance);

                        eventIds.Enqueue(MinigameEventID.PlayerMove);
                        eventArguments.Enqueue(Tuple.Create(eventRegister, true));

                        this.gameInstance.ThreadResume();
                    }
                }
                base.UpdatePosition(playerSession, newPosition);
            }
        }

        public override void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (!inJoinQueue.Contains(playerSession))
            {
                if (configuration.RunBlockQueue)
                {
                    this.gameInstance.Pause();

                    SuperForthInstance.MachineRegister eventRegister = SuperForthInstance.MachineRegister.NewHeapAlloc(this.gameInstance, 5, SuperForthInstance.MachineHeapAllocation.GCTraceMode.None);
                    SuperForthInstance.MachineHeapAllocation placeBlockInfo = eventRegister.HeapAllocation;
                    position.SuperForthSerialize(placeBlockInfo);
                    placeBlockInfo[3] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]);
                    placeBlockInfo[4] = SuperForthInstance.MachineRegister.FromInt(blockType);
                    eventRegister.GCKeepAlive(this.gameInstance);

                    eventIds.Enqueue(MinigameEventID.BlockPlaced);
                    eventArguments.Enqueue(Tuple.Create(eventRegister, true));
                    gameInstance.ThreadResume();

                    if (configuration.DeferBlockPlace)
                        playerSession.SetBlock(position, GetBlock(position));
                    else
                        base.SetBlock(playerSession, position, blockType);
                }
                else
                    base.SetBlock(playerSession, position, blockType);
            }
            else
                playerSession.Message("&eCALM YO TITTIES! You can't build whilst in the join queue.", false);
        }

        public override void MessageFromPlayer(PlayerSession playerSession, string message)
        {
            if (inJoinQueue.Contains(playerSession))
               return;

            if (this.configuration.RunMessageQueue)
            {
                this.gameInstance.Pause();
                
                SuperForthInstance.MachineRegister eventRegister = SuperForthInstance.MachineRegister.NewHeapAlloc(this.gameInstance, 2, SuperForthInstance.MachineHeapAllocation.GCTraceMode.Some);
                SuperForthInstance.MachineHeapAllocation messageInfo = eventRegister.HeapAllocation;
                messageInfo.ConfigureCustomGCTraceSchema(false, true);
                messageInfo[0] = SuperForthInstance.MachineRegister.FromInt((int)playerHandles[playerSession]);
                messageInfo[1] = SuperForthInstance.MachineRegister.FromString(this.gameInstance, message);
                eventRegister.GCKeepAlive(this.gameInstance);

                eventIds.Enqueue(MinigameEventID.PlayerMessage);
                eventArguments.Enqueue(Tuple.Create(eventRegister, true));
                gameInstance.ThreadResume();
            }
            else
                base.MessageFromPlayer(playerSession, message);
        }

        private long NewHandle(PlayerSession playerSession)
        {
            if (playerHandles.ContainsKey(playerSession))
                throw new InvalidOperationException("Player already has a handle.");

            long handle;
            if (unusedHandles.Count > 0)
                handle = unusedHandles.Dequeue();
            else
                handle = this.currentHandle++;
            playerHandles.Add(playerSession, handle);
            handlePlayerMap.Add(handle, playerSession);

            return handle;
        }

        private void FreeHandle(PlayerSession playerSession)
        {
            if (!playerHandles.ContainsKey(playerSession))
                throw new InvalidOperationException("Player doesn't have a handle.");

            unusedHandles.Enqueue(playerHandles[playerSession]);
            handlePlayerMap.Remove(playerHandles[playerSession]);
            playerHandles.Remove(playerSession);
        }

        private int FFIConfigureMinigame(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForthInstance.MachineRegister output)
        {
            if (configured)
                return 0;
            try
            {
                configuration = new MiniGameRuntimeConfiguration(input.HeapAllocation);
                base.Name = configuration.Name;

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

        private int FFIGetQueueTop(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }

            if(joinQueue.Count == 0)
            {
                output.LongInt = -1;
                return 1;
            }

            PlayerSession topPlayer = joinQueue.Dequeue();
            if (topPlayer.Disconnected)
                return FFIGetQueueTop(ref machine, ref input, ref output);
            if (topPlayer.ExtensionManager.SupportsExtension("MessageTypes"))
                topPlayer.SendPacket(new MessagePacket(2, "Your first in the join queue!"));

            output.LongInt = playerHandles.ContainsKey(topPlayer) ? playerHandles[topPlayer] : NewHandle(topPlayer);
            return 1;
        }

        private int FFIAcceptPlayer(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured || !handlePlayerMap.ContainsKey(input.LongInt))
                return 0;

            PlayerSession toAccept = this.handlePlayerMap[input.LongInt];
            inJoinQueue.Remove(toAccept);

            if (toAccept.Disconnected)
            {
                FreeHandle(toAccept);
                output.BoolFlag = false;
                return 1;
            }

            if (this.configuration.RunClickQueue || this.configuration.RunBlockQueue || this.configuration.RunTagQueue)
                try
                {
                    toAccept.OnPlayerClick += this.handlePlayerClick;
                }
                catch (InvalidOperationException)
                {
                    FreeHandle(toAccept);
                    toAccept.Kick("Your client must support player-clicks to play this game.");
                    output.BoolFlag = false;
                    return 1;
                }

            toAccept.ClearPersistantMessages();
            base.JoinWorld(toAccept, PlayerJoinMode.Player);

            output.BoolFlag = true;
            return 1;
        }

        private int FFIRejectPlayer(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }

            if (input.LongInt == -1)
            {
                foreach(PlayerSession playerSession in this.joinQueue)
                {
                    inJoinQueue.Remove(playerSession);
                    FreeHandle(playerSession);
                    playerSession.Kick("All players kicked from join queue by minigame.");
                }
                this.joinQueue.Clear();
                return 1;
            }
            else if (!handlePlayerMap.ContainsKey(input.LongInt))
                return 0;

            PlayerSession toReject = this.handlePlayerMap[input.LongInt];
            inJoinQueue.Remove(toReject);
            FreeHandle(toReject);

            if(!toReject.Disconnected)
                toReject.Kick("You've been kicked off the join queue by the minigame.");
            return 1;
        }

        private int FFIGetEventID(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (eventIds.Count == 0)
                output.LongInt = (long)MinigameEventID.NoneOrEmpty;
            else
                output.LongInt = (long)eventIds.Peek();
            return 1;
        }

        private int FFIGetEventArg(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (eventArguments.Count == 0)
                return 0;
            output = eventArguments.Peek().Item1;
            return 1;
        }

        private int FFINextEvent(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (eventIds.Count == 0)
            {
                output.BoolFlag = false;
                return 1;
            }
            eventIds.Dequeue();

            if(input.BoolFlag) //pop an argument
            {
                if (eventArguments.Count == 0)
                    return 0;

                Tuple<SuperForthInstance.MachineRegister, bool> eventArg =  eventArguments.Dequeue();
                if (eventArg.Item2)
                    eventArg.Item1.GCRelease(gameInstance);
            }
            output.BoolFlag = true;
            return 1;
        }

        private int FFISetActorPlayer(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if (!handlePlayerMap.ContainsKey(input.LongInt))
            {
                Logger.Log("minigame/error", "Minigame tried to set actor to an invalid player handle: " + input.LongInt, this.Name);
                return 0;
            }

            actorPlayer = handlePlayerMap[input.LongInt];
            return 1;
        }

        private void gameThreadLoop()
        {
            this.startTime = DateTime.Now;
            try
            {
                gameInstance.Run();

                while (!stopping) //minigame is paused
                {
                    while (gameInstance.IsPaused) { }
                    gameInstance.ResumeExecute();
                }

                this.exited_thread = true;
                Logger.Log("Info", "The minigame has finished and exited willfully.", this.Name);
                exitHandler.Invoke(this, true);
            }
            catch(SuperForthException error)
            {
                this.exited_thread = true;
                Logger.Log("Info", "A runtime error has occured while running a minigame: " + error.Message, this.Name);
                errorHandler.Invoke(this, error);
                exitHandler.Invoke(this, false);
            }
        }

        public void Dispose() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            Stop();
            Program.worldManager.RemoveWorld(this);

            if (disposing)
            {
                this.gameInstance.Dispose();
            }
        }
    }

    public partial class PlayerPosition
    {
        public PlayerPosition(SuperForthInstance.MachineHeapAllocation machineHeapAllocation) : this((short)machineHeapAllocation[0].LongInt, (short)machineHeapAllocation[1].LongInt, (short)machineHeapAllocation[2].LongInt, (byte)machineHeapAllocation[3].LongInt, (byte)machineHeapAllocation[4].LongInt)
        {
            
        }

        public override void SuperForthSerialize(SuperForthInstance.MachineHeapAllocation heapAllocation)
        {
            base.SuperForthSerialize(heapAllocation);
            heapAllocation[3] = SuperForthInstance.MachineRegister.FromInt(this.Heading);
            heapAllocation[4] = SuperForthInstance.MachineRegister.FromInt(this.Pitch);
        }
    }

    public partial class BlockPosition
    {
        public BlockPosition(SuperForthInstance.MachineHeapAllocation machineHeapAllocation) : this((short)machineHeapAllocation[0].LongInt, (short)machineHeapAllocation[1].LongInt, (short)machineHeapAllocation[2].LongInt)
        {

        }

        public virtual void SuperForthSerialize(SuperForthInstance.MachineHeapAllocation heapAllocation)
        {
            heapAllocation[0] = SuperForthInstance.MachineRegister.FromInt(this.X);
            heapAllocation[1] = SuperForthInstance.MachineRegister.FromInt(this.Y);
            heapAllocation[2] = SuperForthInstance.MachineRegister.FromInt(this.Z);
        }
    }
}