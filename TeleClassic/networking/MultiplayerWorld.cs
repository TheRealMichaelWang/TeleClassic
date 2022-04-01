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
        public static readonly int MaxPlayerCapacity = 127;


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

        public MultiplayerWorld(string fileName, Permission minimumBuildPerms, Permission minimumJoinPerms, int playerCapacity) : base(fileName)
        {
            this.minimumBuildPerms = minimumBuildPerms;
            this.minimumJoinPerms = minimumJoinPerms;

            if (playerCapacity > MultiplayerWorld.MaxPlayerCapacity)
                throw new ArgumentException("Player capacity cannot be greater than 128.", "playerCapacity");
            this.PlayerCapacity = playerCapacity;

            playerIdMap = new Dictionary<PlayerSession, sbyte>(playerCapacity);
            playerPositionMap = new Dictionary<PlayerSession, PlayerPosition>(playerCapacity);
            playersInWorld = new List<PlayerSession>(playerCapacity);
            playerJoinMode = new Dictionary<PlayerSession, PlayerJoinMode>(playerCapacity);

            availibleIds = new Queue<sbyte>(playerCapacity);
            for (sbyte i = 0; i < playerCapacity; i++)
                availibleIds.Enqueue(i);
        }

        public virtual void JoinWorld(PlayerSession playerSession) => JoinWorld(playerSession, PlayerJoinMode.Player);

        public void JoinWorld(PlayerSession playerSession, PlayerJoinMode joinMode)
        {
            if (playerSession.Permissions < minimumJoinPerms)
            {
                playerSession.Kick("Insufficient permissions to join world.");
                Logger.Log("Security", "Player kicked for joining a world they don't have permission to.", playerSession.Name);
                return;
            }
            if(availibleIds.Count == 0)
            {
                playerSession.Kick("World reached capacity of " + PlayerCapacity + ".");
                Logger.Log("Info", "User had attempted to join a world that is full.", this.Name);
                return;
            }
            if (playerIdMap.ContainsKey(playerSession))
            {
                playerSession.Kick("Your Client has a bug: Joining already joined world.");
                Logger.Log("error/gameplay", "Client tried to join world they already joined.", playerSession.Name);
            }

            sbyte id = availibleIds.Dequeue();
            playerIdMap[playerSession] = id;
            playerPositionMap[playerSession] = this.SpawnPoint;
            playerJoinMode[playerSession] = joinMode;

            playerSession.SendWorld(this);
            playerSession.SendPacket(new SpawnPlayerPacket(-1, playerSession.Name, this.SpawnPoint));

            if (joinMode == PlayerJoinMode.Player)
            {
                foreach (PlayerSession otherPlayer in playersInWorld)
                {
                    otherPlayer.SendPacket(new SpawnPlayerPacket(id, playerSession.Name, this.SpawnPoint));
                    playerSession.SendPacket(new SpawnPlayerPacket(playerIdMap[otherPlayer], otherPlayer.Name, playerPositionMap[otherPlayer]));
                }
            }

            playersInWorld.Add(playerSession);
            Logger.Log("Info", "Player joined world " + this.Name + ".", playerSession.Name);
        }

        public virtual void LeaveWorld(PlayerSession playerSession)
        {
            if (!playerIdMap.ContainsKey(playerSession))
                throw new InvalidOperationException("Player has not joined world.");

            sbyte id = playerIdMap[playerSession];
            playersInWorld.Remove(playerSession);

            if (playerJoinMode[playerSession] == PlayerJoinMode.Player)
            {
                foreach (PlayerSession otherPlayer in playersInWorld)
                    otherPlayer.SendPacket(new DespawnPlayerPacket(id));
            }
            
            availibleIds.Enqueue(id);
            playerIdMap.Remove(playerSession);
            playerPositionMap.Remove(playerSession);

            playerSession.ResetHackControl();
            Logger.Log("Info", "Player left world " + this.Name + ".", playerSession.Name);
        }

        public void UpdatePosition(PlayerSession playerSession, PlayerPosition newPosition)
        {
            if (!playerIdMap.ContainsKey(playerSession))
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
