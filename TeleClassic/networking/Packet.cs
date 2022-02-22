using System.Net.Sockets;

namespace TeleClassic.Networking
{
    public abstract class Packet
    {
        public readonly byte OpCode;

        public Packet(byte opCode)
        {
            OpCode = opCode;
        }

        public abstract void Send(NetworkStream stream);
    }
}