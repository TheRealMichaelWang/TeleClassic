using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.networking;

namespace TeleClassic.gameplay
{
    public partial class Player
    {
        public readonly byte ID;
        public readonly string Username;
        public byte UserType;
        public Session ParentSession;
        public Location Location;

        public Player(byte playerID, byte userType, string username, Session parentSession, Location location)
        {
            this.ID = playerID;
            this.UserType = userType;
            this.Username = username;
            this.ParentSession = parentSession;
            this.Location = location;
        }
    }

    public class PlayerManager
    {
        public static int MaxPlayerCount = 100;

        Stack<byte> freePlayerIDs;
        HashSet<byte> allocatedPlayerIDs;
        List<Player> players;

        public int PlayerCount { get; private set; }

        public Player this[byte id]
        { 
            get
            {
                return SearchForPlayer(id);
            }
        }

        public PlayerManager()
        {
            freePlayerIDs = new Stack<byte>();
            allocatedPlayerIDs = new HashSet<byte>();
            players = new List<Player>();
            PlayerCount = 0;
            for (byte i = 0; i < MaxPlayerCount; i++)
            {
                freePlayerIDs.Push(i);
            }
        }

        public Player CreatePlayer(string username, byte userType, Session parentSession, Location location)
        {
            byte freeID = freePlayerIDs.Pop();
            allocatedPlayerIDs.Add(freeID);
            Player player = new Player(freeID, userType, username, parentSession, location);
            players.Add(player);
            Console.WriteLine(username + "(ID:" + freeID + ") has joined the server.");
            return player;
        }

        public Player RemovePlayer(byte id)
        {
            if (!allocatedPlayerIDs.Contains(id))
            {
                throw new InvalidOperationException("Invalid player id specified.");
            }
            Player toRemove = null;
            foreach(Player player in players)
            {
                if(player.ID == id)
                {
                    toRemove = player;
                    break;
                }
            }
            players.Remove(toRemove);
            allocatedPlayerIDs.Remove(id);
            freePlayerIDs.Push(id);
            return toRemove;
        }

        public Player SearchForPlayer(byte id)
        {
            if(!allocatedPlayerIDs.Contains(id))
            {
                throw new InvalidOperationException("Invalid player id specified.");
            }
            foreach(Player player in players)
            {
                if(player.ID == id)
                {
                    return player;
                }
            }
            //The ID should ALWAYS be found.
            throw new KeyNotFoundException();
        }
    }
}
