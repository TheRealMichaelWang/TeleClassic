using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TeleClassic.Networking
{
    public sealed class MinecraftStreamWriter
    {
        private readonly BinaryWriter writer;

        public MinecraftStreamWriter(NetworkStream stream)
        {
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
        }

        public void WriteByte(byte b) => writer.Write(b);
        public void WriteSByte(sbyte b) => writer.Write(b);

        public void WriteShort(short s) => writer.Write(IPAddress.HostToNetworkOrder(s));
        public void WriteInt(int i) => writer.Write(IPAddress.HostToNetworkOrder(i));

        public void WriteString(string s)
        {
            if (s.Length > 64)
                s = s.Substring(0, 64);
            foreach (char c in s)
                writer.Write(c);
            for (int i = s.Length; i < 64; i++)
                WriteByte(0x20);
        }
    }

    public sealed class MinecraftStreamReader
    {
        private readonly BinaryReader reader;

        public MinecraftStreamReader(NetworkStream stream)
        {
            reader = new BinaryReader(stream, Encoding.UTF8, true);
        }

        public byte ReadByte() => reader.ReadByte();
        public sbyte ReadSByte() => reader.ReadSByte();

        public short ReadShort() => IPAddress.NetworkToHostOrder(reader.ReadInt16());
        public int ReadInt() => IPAddress.NetworkToHostOrder(reader.ReadInt32());

        public string ReadString()
        {
            char[] buffer = new char[64];
            for (int i = 0; i < 64; i++)
                buffer[i] = reader.ReadChar();
            return new string(buffer).Trim();
        }
    }
}