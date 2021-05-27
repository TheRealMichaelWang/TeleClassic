using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.networking.protocol
{
    public class Packet : IDisposable
    {
        private bool _disposed;
        private MemoryStream stream;

        public readonly byte PacketID;

        public Packet(byte[] data)
        {
            stream = new MemoryStream(data);
            _disposed = false;
            PacketID = ReadByte();
        }

        public Packet(byte packetID)
        {
            stream = new MemoryStream();
            _disposed = false;
            this.PacketID = packetID;
            WriteByte(packetID);
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }
            if(disposing)
            {
                stream.Dispose();
            }
            _disposed = true;
        }

        public byte ReadByte()
        {
            return (byte)stream.ReadByte();
        }

        public sbyte ReadSignedByte()
        {
            return (sbyte)stream.ReadByte();
        }

        public short ReadShort()
        {
            short s = BitConverter.ToInt16(new byte[] { ReadByte(), ReadByte() }, 0);
            return IPAddress.NetworkToHostOrder(s);
        }

        public string ReadString()
        {
            char[] char_buffer = new char[64];
            for (int i = 0; i < 64; i++)
            {
                char_buffer[i] = (char)ReadByte();
            }
            return new string(char_buffer);
        }

        public byte[] ReadByteArray()
        {
            byte[] buffer = new byte[1024];
            for (int i = 0; i < 1024; i++)
            {
                buffer[i] = ReadByte();
            }
            return buffer;
        }

        public void WriteByte(byte b)
        {
            stream.WriteByte(b);
        }

        public void WriteSignedByte(sbyte b)
        {
            stream.WriteByte((byte)b);
        }

        public void WriteShort(short s)
        {
            s = IPAddress.HostToNetworkOrder(s);
            stream.Write(BitConverter.GetBytes(s), 0, 2);
        }

        public void WriteString(string s)
        {
            if(s.Length > 64)
            {
                throw new InvalidOperationException("Cannot send string longer than 64 characters.");
            }
            for (int i = 0; i < s.Length; i++)
            {
                stream.WriteByte((byte)s[i]);
            }
            for (int i = s.Length; i < 64; i++)
            {
                stream.WriteByte(0x20);
            }
        }

        public void WriteByteArray(byte[] data)
        {
            if(data.Length > 1024)
            {
                throw new InvalidOperationException("Cannot send byte array with a length greater than 1024.");
            }
            for (int i = 0; i < data.Length; i++)
            {
                WriteByte(data[i]);
            }
            for (int i = data.Length; i < 1024; i++)
            {
                WriteByte(0);
            }
        }

        public byte[] ToByteArray()
        {
            return stream.ToArray();
        }
    }
}
