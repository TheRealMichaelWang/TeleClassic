using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.gameplay.world;
using TeleClassic.networking.protocol;
using TeleClassic.networking.protocol.clientbound;

namespace TeleClassic.gameplay
{
    public partial class MultiplayerWorld
    {
        public string Name { get; private set; }
        public bool Locked { get; private set; }

        HashSet<Player> players;
        World world;

        public byte this[short x, short y, short z]
        { 
            get
            {
                return GetBlock(x, y, z);
            }
            set
            {
                SetBlock(x, y, z, value);
            }
        }

        public MultiplayerWorld(string name, World world, bool locked)
        {
            this.Name = name;
            this.world = world;
            this.Locked = locked;
            this.players = new HashSet<Player>();
        }

        public void JoinWorld(Player player)
        {
            if(player.Location != null)
            {
                throw new InvalidOperationException("Please leave the world you are currentley in before joining another.");
            }
            if(players.Contains(player))
            {
                throw new InvalidOperationException("Cannot join the same world. Please leave then rejoin.");
            }
            player.Location = new Location(Name, world.SpawnPoint.Clone() as Position);
            player.SendWorldData(world);
            SpawnPlayer(player);
            players.Add(player);
        }

        public void LeaveWorld(Player player)
        {
            player.Location = null;
            if(!players.Contains(player))
            {
                throw new InvalidOperationException("Cannot remove a player outside of this world.");
            }
            players.Remove(player);
            Gameplay.ExecuteTask(DespawnPlayer, player);
        }

        public void Broadcast(object[] args) => Broadcast((string)args[0]);

        public void Broadcast(string message)
        {
            foreach(Player player in players)
            {
                player.Message(message);
            }
        }

        public void UpdatePosition(object[] args) => UpdatePosition((Player)args[0]);

        public void UpdatePosition(Player player)
        {
            if(!players.Contains(player))
            {
                throw new InvalidOperationException("Cannot update a position of a player outside of this world.");
            }
            foreach(Player toUpdate in players)
            {
                if(toUpdate != player)
                {
                    toUpdate.ParentSession.SendPacket(new PositionAndOrientationPacket(Convert.ToSByte(player.ID), player.Location.Position.X, player.Location.Position.Y, player.Location.Position.Z, player.Location.Position.Yaw, player.Location.Position.Pitch));
                }
            }
        }

        private void SpawnPlayer(object[] args) => SpawnPlayer((Player)args[0]);

        private void SpawnPlayer(Player tospawn)
        {
            tospawn.ParentSession.SendPacket(new SpawnPlayerPacket(255, tospawn.Username, tospawn.Location.Position.X, tospawn.Location.Position.Y, tospawn.Location.Position.Z, tospawn.Location.Position.Yaw, tospawn.Location.Position.Pitch));
            foreach(Player player in players)
            {
                player.ParentSession.SendPacket(new SpawnPlayerPacket(tospawn.ID, tospawn.Username, tospawn.Location.Position.X, tospawn.Location.Position.Y, tospawn.Location.Position.Z, tospawn.Location.Position.Yaw, tospawn.Location.Position.Pitch));
                tospawn.ParentSession.SendPacket(new SpawnPlayerPacket(player.ID, player.Username, player.Location.Position.X, player.Location.Position.Y, player.Location.Position.Z, player.Location.Position.Yaw, player.Location.Position.Pitch));
            }
        }

        private void DespawnPlayer(object[] args) => DespawnPlayer((Player)args[0]);

        private void DespawnPlayer(Player todespawn)
        {
            foreach(Player player in players)
            {
                player.ParentSession.SendPacket(new DespawnPlayerPacket(Convert.ToSByte(todespawn.ID)));
            }
        }

        public byte GetBlock(short x, short y, short z) => world.GetBlock(x, y, z);

        public void SetBlock(short x, short y, short z, byte block)
        {
            if(Locked)
            {
                throw new InvalidOperationException("You cannot modify a locked world.");
            }
            world.SetBlock(x, y, z, block);
            Gameplay.ExecuteTask(UpdateBlock, x, y, z, block);
        }

        private void UpdateBlock(object[] args) => UpdateBlock((short)args[0], (short)args[1], (short)args[2], (byte)args[3]);

        private void UpdateBlock(short x, short y, short z, byte block)
        {
            foreach (Player player in players)
            {
                player.ParentSession.SendPacket(new SetBlockPacket(x, y, z, block));
            }
        }
    }
}
