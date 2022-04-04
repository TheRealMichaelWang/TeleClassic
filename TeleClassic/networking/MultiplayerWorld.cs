using System;
using System.Collections.Generic;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Networking
{
    public partial class MultiplayerWorld : World
    {
        public enum PlayerJoinMode
        {
            Player,
            Spectator
        }

        public sealed class GetPlayerListCommandAction : CommandProcessor.CommandAction
        {
            public int GetExpectedArgumentCount() => 1;
            public bool ReturnsValue() => true;

            public string GetName() => "lp";
            public string GetDescription() => "Lists players in worlds.";

            public void Invoke(CommandProcessor commandProcessor)
            {
                CommandProcessor.WorldCommandObject worlds = (CommandProcessor.WorldCommandObject)commandProcessor.PopObject(typeof(CommandProcessor.WorldCommandObject));

                List<PlayerSession> playersInWorld = new List<PlayerSession>();
                foreach (MultiplayerWorld world in worlds.worlds)
                {
                    playersInWorld.AddRange(world.playersInWorld);
                }
                commandProcessor.PushObject(new CommandProcessor.PlayerCommandObject(playersInWorld));
            }
        }

        public static readonly GetPlayerListCommandAction getPlayerListCommandAction = new GetPlayerListCommandAction();
        private static readonly int MaxPlayerCapacity = 127;


        private List<PlayerSession> playersInWorld;
        
        private Dictionary<PlayerSession, sbyte> playerIdMap;
        private Dictionary<PlayerSession, PlayerPosition> playerPositionMap;
        private Dictionary<PlayerSession, PlayerJoinMode> playerJoinMode;
        private Queue<sbyte> availibleIds;

        private Permission minimumBuildPerms;
        private Permission minimumJoinPerms;

        public readonly int PlayerCapacity;

        public bool InWorld(PlayerSession player) => playerIdMap.ContainsKey(player);
        public PlayerPosition GetPlayerPosition(PlayerSession player) => playerPositionMap[player];

        public MultiplayerWorld(string name, string fileName, Permission minimumBuildPerms, Permission minimumJoinPerms, int playerCapacity) : base(name, fileName)
        {
            this.minimumBuildPerms = minimumBuildPerms;
            this.minimumJoinPerms = minimumJoinPerms;

            this.PlayerCapacity = playerCapacity;

            int idCap = Math.Min(MultiplayerWorld.MaxPlayerCapacity, playerCapacity);
            playerPositionMap = new Dictionary<PlayerSession, PlayerPosition>(idCap);
            playersInWorld = new List<PlayerSession>(idCap);
            playerJoinMode = new Dictionary<PlayerSession, PlayerJoinMode>(idCap);

            playerIdMap = new Dictionary<PlayerSession, sbyte>(idCap);
            availibleIds = new Queue<sbyte>(idCap);
            for (sbyte i = 0; i < idCap; i++)
                availibleIds.Enqueue(i);
        }

        public MultiplayerWorld(string fileName, Permission minimumBuildPerms, Permission minimumJoinPerms, int playerCapacity) : this(fileName, fileName, minimumBuildPerms, minimumJoinPerms, playerCapacity)
        {
            
        }

        public virtual void JoinWorld(PlayerSession playerSession)
        {
            if (availibleIds.Count > 0)
            {
                JoinWorld(playerSession, PlayerJoinMode.Player);
            }
            else
            {
                Logger.Log("error/gameplay", "No avalible player id's, joining player as spectator.", playerSession.Name);
                JoinWorld(playerSession, PlayerJoinMode.Spectator);
            }
        }

        public void JoinWorld(PlayerSession playerSession, PlayerJoinMode joinMode)
        {
            if (playerSession.Permissions < minimumJoinPerms)
            {
                Logger.Log("Security", "Player kicked for joining a world they don't have permission to.", playerSession.Name);
                playerSession.Kick("Insufficient permissions to join world.");
                return;
            }
            if(playersInWorld.Count == PlayerCapacity)
            {
                Logger.Log("Info", "User had attempted to join a world that is full.", this.Name);
                playerSession.Kick("World reached capacity of " + PlayerCapacity + ".");
                return;
            }
            if (playerJoinMode.ContainsKey(playerSession))
            {
                Logger.Log("error/gameplay", "Client tried to join world they already joined.", playerSession.Name);
                playerSession.Kick("Your Client has a bug: Joining already joined world.");
                return;
            }
            if(joinMode == PlayerJoinMode.Player && availibleIds.Count == 0)
            {
                Logger.Log("error/gameplay", "Could not fullfill client rquest to join as player: No more availible player id's.", playerSession.Name);
                playerSession.Kick("Could not fullfill request to jojn as player.");
                return;
            }

            playerPositionMap[playerSession] = this.SpawnPoint;
            playerJoinMode[playerSession] = joinMode;

            playerSession.SendWorld(this);
            playerSession.SendPacket(new SpawnPlayerPacket(-1, playerSession.Name, this.SpawnPoint));

            if (joinMode == PlayerJoinMode.Player)
            {
                sbyte id = availibleIds.Dequeue();
                playerIdMap[playerSession] = id;
                foreach (PlayerSession otherPlayer in playersInWorld)
                {
                    otherPlayer.SendPacket(new SpawnPlayerPacket(id, playerSession.Name, this.SpawnPoint));
                    playerSession.SendPacket(new SpawnPlayerPacket(playerIdMap[otherPlayer], otherPlayer.Name, playerPositionMap[otherPlayer]));
                }
            }

            playersInWorld.Add(playerSession);
            Logger.Log("Info", "Player joined world " + this.Name + " as " + joinMode + ".", playerSession.Name);
        }

        public virtual void LeaveWorld(PlayerSession playerSession)
        {
            if (!playerJoinMode.ContainsKey(playerSession))
                throw new InvalidOperationException("Player has not joined world.");

            playersInWorld.Remove(playerSession);
            playerPositionMap.Remove(playerSession);
            if (playerJoinMode[playerSession] == PlayerJoinMode.Player)
            {
                sbyte id = playerIdMap[playerSession];
                foreach (PlayerSession otherPlayer in playersInWorld)
                    otherPlayer.SendPacket(new DespawnPlayerPacket(id));
                availibleIds.Enqueue(id);
                playerIdMap.Remove(playerSession);
            }
            playerJoinMode.Remove(playerSession);
            
            playerSession.ResetHackControl();
            Logger.Log("Info", "Player left world " + this.Name + ".", playerSession.Name);
        }

        public virtual void UpdatePosition(PlayerSession playerSession, PlayerPosition newPosition)
        {
            if (!playerJoinMode.ContainsKey(playerSession))
                throw new InvalidOperationException("Player has not joined world.");

            playerPositionMap[playerSession] = newPosition;

            if (playerJoinMode[playerSession] == PlayerJoinMode.Player)
            {
                foreach (PlayerSession otherPlayer in playersInWorld)
                    if (otherPlayer != playerSession)
                        otherPlayer.SendPacket(new PositionAndOrientationPacket(playerIdMap[playerSession], newPosition));
            }
        }

        public virtual void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (playerSession.Permissions < this.minimumBuildPerms)
            {
                playerSession.Message("&cInsufficient permissions to build.", false);
                playerSession.SetBlock(position, GetBlock(position));
                return;
            }
            while (bulkBlockUpdateMode){}

            base.SetBlock(position, blockType);
            foreach (PlayerSession otherPlayer in playersInWorld)
                if (otherPlayer != playerSession)
                    otherPlayer.SetBlock(position, blockType);
        }

        public void MessageAllPlayers(string message)
        {
            foreach (PlayerSession player in playersInWorld)
                player.Message(message, false);
        }

        public void MessageAllPlayers(MessagePacket messagePacket)
        {
            foreach (PlayerSession player in playersInWorld)
                player.SendPacket(messagePacket);
        }

        public void MessageFromPlayer(PlayerSession playerSession, string message)
        {
            if (playerSession.IsMuted)
            {
                playerSession.Message("&cShut the fuck up, you are muted.", false);
                return;
            }
            MessageAllPlayers("&a" + playerSession.Name+":&e"+ message);
        }
    }
}
