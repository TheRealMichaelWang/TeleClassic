using System;
using System.Collections.Generic;
using TeleClassic.Gameplay;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Networking
{
    public class MultiplayerWorld : World
    {
        private List<PlayerSession> playersInWorld;
        private Dictionary<PlayerSession, sbyte> playerIdMap;
        private Dictionary<PlayerSession, PlayerPosition> playerPositionMap;
        private Queue<sbyte> availibleIds;

        private Permission minimumBuildPerms;
        private Permission minimumJoinPerms;

        public int PlayerCapacity { get; private set; }

        public MultiplayerWorld(string fileName, Permission minimumBuildPerms, Permission minimumJoinPerms, int playerCapacity) : base(fileName)
        {
            this.minimumBuildPerms = minimumBuildPerms;
            this.minimumJoinPerms = minimumJoinPerms;
            this.PlayerCapacity = playerCapacity;

            playersInWorld = new List<PlayerSession>(playerCapacity);
            playerIdMap = new Dictionary<PlayerSession, sbyte>(playerCapacity);
            playerPositionMap = new Dictionary<PlayerSession, PlayerPosition>(playerCapacity);

            if (playerCapacity > 127)
                throw new ArgumentException("Player capacity cannot be greater than 128.", "playerCapacity");
            availibleIds = new Queue<sbyte>(playerCapacity);
            for (sbyte i = 0; i < playerCapacity; i++)
                availibleIds.Enqueue(i);
        }

        public void JoinWorld(PlayerSession playerSession)
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

            playerSession.SendWorld(this);
            playerSession.SendPacket(new SpawnPlayerPacket(-1, playerSession.Name, this.SpawnPoint));
            foreach (PlayerSession otherPlayer in playersInWorld)
            {
                otherPlayer.SendPacket(new SpawnPlayerPacket(id, playerSession.Name, this.SpawnPoint));
                playerSession.SendPacket(new SpawnPlayerPacket(playerIdMap[otherPlayer], otherPlayer.Name, playerPositionMap[otherPlayer]));
            }

            playersInWorld.Add(playerSession);
            Logger.Log("Info", "Player joined world " + this.Name + ".", playerSession.Name);
        }

        public void LeaveWorld(PlayerSession playerSession)
        {
            if (!playerIdMap.ContainsKey(playerSession))
                throw new InvalidOperationException("Player has not joined world.");

            sbyte id = playerIdMap[playerSession];
            playersInWorld.Remove(playerSession);
            foreach (PlayerSession otherPlayer in playersInWorld)
                otherPlayer.SendPacket(new DespawnPlayerPacket(id));
            availibleIds.Enqueue(id);
            playerIdMap.Remove(playerSession);
            playerPositionMap.Remove(playerSession);
            Logger.Log("Info", "Player left world " + this.Name + ".", playerSession.Name);
        }

        public void UpdatePosition(PlayerSession playerSession, PlayerPosition newPosition)
        {
            if (!playerIdMap.ContainsKey(playerSession))
                throw new InvalidOperationException("Player has not joined world.");

            playerPositionMap[playerSession] = newPosition;
            foreach (PlayerSession otherPlayer in playersInWorld)
                if (otherPlayer != playerSession)
                    otherPlayer.SendPacket(new PositionAndOrientationPacket(playerIdMap[playerSession], newPosition));
        }

        public virtual void SetBlock(PlayerSession playerSession, BlockPosition position, byte blockType)
        {
            if (playerSession.Permissions < this.minimumBuildPerms)
            {
                playerSession.Message("Insufficient permissions to build.");
                playerSession.SendPacket(new SetBlockPacket(position, GetBlock(position)));
                return;
            }
            base.SetBlock(position, blockType);
            foreach (PlayerSession otherPlayer in playersInWorld)
                if (otherPlayer != playerSession)
                    otherPlayer.SendPacket(new SetBlockPacket(position, blockType));
        }
    }
}
