using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.Networking;
using TeleClassic.Networking.CEP;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic
{
    public sealed class Lobby : MultiplayerWorld
    {
        public Lobby() : base("lobby", "templobby.cw", Permission.Admin, Permission.Member, int.MaxValue)
        {
                
        }

        public override void JoinWorld(PlayerSession playerSession)
        {
            base.JoinWorld(playerSession);
            if(playerSession.ExtensionManager.SupportsExtension("HackControl"))
                playerSession.SendPacket(new HackControlPacket(playerSession.Permissions == Permission.Admin, playerSession.Permissions == Permission.Admin, true, false, true, 300));
            if (playerSession.ExtensionManager.SupportsExtension("MessageTypes"))
            {
                playerSession.SendPacket(new MessagePacket(1, playerSession.IsLoggedIn ? "Logged in as " + playerSession.Account.Username : "Logged in as guest"));
                playerSession.SendPacket(new MessagePacket(2, "Your Rank: " + playerSession.Permissions));

                playerSession.SendPacket(new MessagePacket(12, "Worlds on Server: " + Program.worldManager.WorldsOnServer));
                playerSession.SendPacket(new MessagePacket(11, "Minigames on Server: " + Program.miniGameMarshaller.ActiveMinigames));
                playerSession.SendPacket(new MessagePacket(13, "Players on Server: " + Program.server.PlayerCount));
            }
            playerSession.Announce("Welcome to TeleClassic");
        }
    }
}
