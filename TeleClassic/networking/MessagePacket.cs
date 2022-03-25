using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

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
                Message(command, true);
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