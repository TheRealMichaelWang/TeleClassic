using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TeleClassic.gameplay;
using TeleClassic.networking.protocol;
using TeleClassic.networking.protocol.clientbound;

namespace TeleClassic.networking
{
    public partial class Session
    {
        public bool Closed { get; private set; }

        TcpClient client;
        NetworkStream networkStream;

        public bool PacketsAvailible() => networkStream.DataAvailable;

        public Session(TcpClient client)
        {
            this.client = client;
            networkStream = client.GetStream();
            Closed = false;
            player = null; //set null to flag as uninitialized
        }

        public void SendPacket(Packet packet)
        {
            byte[] data = packet.ToByteArray();
            networkStream.Write(data, 0, data.Length);
            packet.Dispose();
        }

        public Packet WaitForPacket()
        {
            //wait for packet
            while(!PacketsAvailible())
            {

            }
            Packet packet;
            using (MemoryStream stream = new MemoryStream())
            {
                while (networkStream.DataAvailable) //wait and buffer the data
                {
                    byte[] tempbuffer = new byte[client.Available];
                    networkStream.Read(tempbuffer, 0, tempbuffer.Length);
                    stream.Write(tempbuffer, 0, tempbuffer.Length);
                }
                packet = new Packet(stream.ToArray());
            }
            return packet;
        }

        public bool SendPing()
        {
            try
            {
                SendPacket(new PingPacket());
            }
            catch
            {
                Close();
            }
            return !Closed;
        }

        public void Close()
        {
            if(Closed)
            {
                return;
            }
            if(player != null)
            {
                player.Destroy();
            }
            networkStream.Close();
            client.Close();
            Closed = true;
        }
    }
}
