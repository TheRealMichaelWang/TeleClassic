using SuperForth;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TeleClassic.Networking;

namespace TeleClassic.Networking
{
    public sealed class MessagePacket : Packet
    {
        public readonly sbyte PlayerID;
        public readonly string Message;

        public MessagePacket(sbyte playerID, string message) : base(0x0d)
        {
            PlayerID = playerID;
            Message = message;
        }

        public MessagePacket(NetworkStream stream) : base(0x0d)
        {
            MinecraftStreamReader reader = new MinecraftStreamReader(stream);
            PlayerID = reader.ReadSByte();
            Message = reader.ReadString();
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x0d);
            writer.WriteSByte(PlayerID);
            writer.WriteString(Message);
        }
    }

    public partial class PlayerSession
    {
        Queue<string> messageBacklog = new Queue<string>();

        public void Announce(string announcement)
        {
            if (this.ExtensionManager.SupportsExtension("MessageTypes"))
                SendPacket(new MessagePacket(100, announcement));
            else
                SendPacket(new MessagePacket(-1, announcement));
        }

        public void ClearPersistantMessages()
        {
            if (ExtensionManager.SupportsExtension("MessageTypes"))
            {
                SendPacket(new MessagePacket(1, string.Empty));
                SendPacket(new MessagePacket(2, string.Empty));
                SendPacket(new MessagePacket(3, string.Empty));

                SendPacket(new MessagePacket(11, string.Empty));
                SendPacket(new MessagePacket(12, string.Empty));
                SendPacket(new MessagePacket(13, string.Empty));
            }
        }

        public void Message(string message, bool backlogged)
        {
            message = message.Replace("\r", string.Empty);
            if (message.Contains("\n"))
            {
                foreach (string line in message.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    Message(line, backlogged);
            }
            else
            {
                for (int i = 0; i < message.Length; i += 64)
                {
                    if (backlogged)
                        messageBacklog.Enqueue(message.Substring(i, Math.Min(message.Length - i, 64)));
                    else
                        SendRawMessage(message.Substring(i, Math.Min(message.Length - i, 64)));
                }
            }
        }

        private void SendRawMessage(string message)
        {
            if(this.ExtensionManager.SupportsExtension("MessageTypes"))
                SendPacket(new MessagePacket(0, message));
            else
                SendPacket(new MessagePacket(-1, message));
        }

        public void EmptyMessageBacklog()
        {
            if (messageBacklog.Count == 0) {
                SendRawMessage("No messages from server, backlog empty.");
                return;
            }
            for (int i = 0; i < 9 && messageBacklog.Count > 0; i++)
                SendRawMessage(messageBacklog.Dequeue());
            if(messageBacklog.Count > 0)
                SendRawMessage("Type `next` or press CTRL+N to read " + messageBacklog.Count + " more message(s).");
        }

        public void handlePlayerMessage()
        {
            MessagePacket messagePacket = new MessagePacket(networkStream);
            if (currentWorld == null)
            {
                Logger.Log("error/networking", "Client tried to send a message but hasn't joined a world.", Address.ToString());
                throw new InvalidOperationException("Your Client has a bug: setting blocks whilst not in world.");
            }

            if (messagePacket.Message.StartsWith('.') || messagePacket.Message.StartsWith('/'))
            {
                messageBacklog.Clear();
                string command = messagePacket.Message.TrimStart('.', '/');
                Message("&d"+command, true);
                try
                {
                    commandProcessor.ExecuteCommand(CommandParser.Compile(command));
                    EmptyMessageBacklog();
                }
                catch (ArgumentException e)
                {
                    Message(e.Message, false);
                }
            }
            else if (messagePacket.Message == "next")
                EmptyMessageBacklog();
            else
            {
                currentWorld.MessageFromPlayer(this, messagePacket.Message);
            }
        }
    }
}

namespace TeleClassic.Gameplay
{
    public partial class MiniGame
    {
        SuperForthInstance.Machine.ForeignFunction sendUrgentMessageDelegate;
        SuperForthInstance.Machine.ForeignFunction sendBackloggedMessageDelegate;
        SuperForthInstance.Machine.ForeignFunction playerAnnounceDelegate;
        SuperForthInstance.Machine.ForeignFunction setPlayerStatusMessagePositionDelegate;
        SuperForthInstance.Machine.ForeignFunction setPlayerStatusMessageDelegate;

        private sbyte statusMessagePosition = 1;

        private int FFISendUrgentMessage(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if(actorPlayer == null)
            {
                Logger.Log("minigame/error", "Minigame invoked message player while actor player was unset.", this.Name);
                return 0;
            }
            if (actorPlayer.Disconnected)
            {
                Logger.Log("minigame/error", "Actor player was disconnected, could not message.", this.Name);
                return 0;
            }

            actorPlayer.Message(input.HeapAllocation.GetString(), false);
            return 1;
        }

        private int FFISendBackloggedMessage(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if (actorPlayer == null)
            {
                Logger.Log("minigame/error", "Minigame invoked message player while actor player was unset.", this.Name);
                return 0;
            }
            if (actorPlayer.Disconnected)
            {
                Logger.Log("minigame/error", "Actor player was disconnected, could not message.", this.Name);
                return 0;
            }

            actorPlayer.Message(input.HeapAllocation.GetString(), true);
            actorPlayer.EmptyMessageBacklog();
            return 1;
        }

        private int FFIPlayerAnnounce(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if (actorPlayer == null)
            {
                Logger.Log("minigame/error", "Minigame invoked message player while actor player was unset.", this.Name);
                return 0;
            }
            if (actorPlayer.Disconnected)
            {
                Logger.Log("minigame/error", "Actor player was disconnected, could not message.", this.Name);
                return 0;
            }

            string messageToSend = input.HeapAllocation.GetString();
            if(messageToSend.Length > 64)
            {
                Logger.Log("minigame/error", "Minigame tried to send an announcement longer than 64 chars.", this.Name);
                return 0;
            }
            actorPlayer.Announce(messageToSend);

            return 1;
        }

        private int FFISetPlayerStatusMessagePosition(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if (actorPlayer == null)
            {
                Logger.Log("minigame/error", "Minigame invoked message player while actor player was unset.", this.Name);
                return 0;
            }
            if (actorPlayer.Disconnected)
            {
                Logger.Log("minigame/error", "Actor player was disconnected, could not message.", this.Name);
                return 0;
            }
            if(!((input.LongInt >= 1 && input.LongInt <= 3) || (input.LongInt >= 10 && input.LongInt <= 13)))
            {
                Logger.Log("minigame/error", "Minigame sent invalid player status message position: " + input.LongInt, this.Name);
                return 0;
            }
            this.statusMessagePosition = (sbyte)input.LongInt;
            return 1;
        }

        private int FFISetPlayerStatusMessage(ref SuperForthInstance.Machine machine, ref SuperForthInstance.MachineRegister input, ref SuperForth.SuperForthInstance.MachineRegister output)
        {
            if (!configured)
            {
                Logger.Log("minigame/error", "Minigame tried to log before finishing configuration.", this.Name);
                return 0;
            }
            if (actorPlayer == null)
            {
                Logger.Log("minigame/error", "Minigame invoked message player while actor player was unset.", this.Name);
                return 0;
            }
            if (actorPlayer.Disconnected)
            {
                Logger.Log("minigame/error", "Actor player was disconnected, could not message.", this.Name);
                return 0;
            }

            if (!actorPlayer.ExtensionManager.SupportsExtension("MessageTypes"))
            {
                Logger.Log("minigame/error/nonfatal", "Actor player doesn't support message types.", this.Name);
                output.BoolFlag = false;
                return 1;
            }

            actorPlayer.SendPacket(new MessagePacket(this.statusMessagePosition, input.HeapAllocation.GetString()));
            output.BoolFlag = true;
            return 1;
        }
    }
}