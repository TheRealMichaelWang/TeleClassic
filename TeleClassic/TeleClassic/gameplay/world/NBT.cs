using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeleClassic.gameplay.world
{
    class NBTTag
    {
        public string Name;
        public byte Tag;
        public object Value;

        public NBTTag(string name, byte tag, object value)
        {
            this.Name = name;
            this.Tag = tag;
            this.Value = value;
        }
    }

    class NBTCompound
    {
        public List<NBTTag> Tags { get; private set; }

        public NBTTag this[string name]
        { 
            get
            {
                foreach(NBTTag tag in Tags)
                {
                    if(tag.Name == name)
                    {
                        return tag;
                    }
                }
                throw new KeyNotFoundException(name);
            }
        }

        public int TagCount
        {
            get => Tags.Count;
        }

        public NBTCompound()
        {
            Tags = new List<NBTTag>();
        }

        public void AddTag(NBTTag tag)
        {
            Tags.Add(tag);
        }
    }

    class NBT
    {
        NBTCompound head;

        public NBT(MemoryStream stream)
        {
            head = ReadCompound(stream);
        }

        public NBT()
        {
            head = new NBTCompound();
        }

        public void Write(MemoryStream stream)
        {
            WriteCompound(head, stream);
        }

        public bool HasObject(string path)
        {
            string[] parts = path.Split('/');
            int current_part = 0;
            NBTTag current = new NBTTag("HEAD", 10, this.head);
            while (current_part < parts.Length)
            {
                bool notfound = true;
                foreach (NBTTag tag in (current.Value as NBTCompound).Tags)
                {
                    if (tag.Name == parts[current_part])
                    {
                        current = tag;
                        notfound = false;
                        break;
                    }
                }
                if (notfound)
                {
                    return false;
                }
                else
                {
                    current_part++;
                }
            }
            return true;
        }

        public object GetObject(string path)
        {
            string[] parts = path.Split('/');
            int current_part = 0;
            NBTTag current = new NBTTag("HEAD", 10, this.head);
            while (current_part < parts.Length) 
            {
                bool notfound = true;
                foreach (NBTTag tag in (current.Value as NBTCompound).Tags)
                {
                    if (tag.Name == parts[current_part])
                    {
                        current = tag;
                        notfound = false;
                        break;
                    }
                }
                if (notfound)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    current_part++;
                }
            }
            return current.Value;
        }

        public void SetObject(string path, object value)
        {
            string[] parts = path.Split('/');
            int current_part = 0;
            NBTTag current = new NBTTag("HEAD", 10, this.head);
            while (current_part < parts.Length)
            {
                bool notfound = true;
                foreach (NBTTag tag in (current.Value as NBTCompound).Tags)
                {
                    if (tag.Name == parts[current_part])
                    {
                        current = tag;
                        notfound = false;
                        break;
                    }
                }
                if (notfound)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    current_part++;
                }
            }
            current.Value = value;
        }

        public void AddCompound(string path, string name)
        {
            AddTag(path, new NBTTag(name, 10, new NBTCompound()));
        }

        public void AddValue(string path, string name, byte type, object value)
        {
            AddTag(path, new NBTTag(name, type, value));
        }

        private void AddTag(string path, NBTTag toadd)
        {
            string[] parts = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int current_part = 0;
            NBTTag current = new NBTTag("HEAD", 10, this.head);
            while (current_part < parts.Length)
            {
                bool notfound = true;
                foreach (NBTTag tag in (current.Value as NBTCompound).Tags)
                {
                    if (tag.Name == parts[current_part])
                    {
                        current = tag;
                        notfound = false;
                        break;
                    }
                }
                if (notfound)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    current_part++;
                }
            }
            if(!(current.Value is NBTCompound))
            {
                throw new InvalidOperationException("You can only add tags to NBT compounds.");
            }
            (current.Value as NBTCompound).AddTag(toadd);
        }

        private short ReadShort(MemoryStream stream)
        {
            byte[] data = new byte[2];
            stream.Read(data, 0, 2);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        }

        private void WriteShort(short s, MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(s)), 0, 2);
        }

        private int ReadInt(MemoryStream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, 4);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data,0));
        }

        private void WriteInt(int i, MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i)), 0, 4);
        }

        private long ReadLong(MemoryStream stream)
        {
            byte[] data = new byte[8];
            stream.Read(data, 0, 8);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, 0));
        }

        private void WriteLong(long l, MemoryStream stream)
        {
            stream.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(l)), 0, 8);
        }

        private string ReadString(MemoryStream stream)
        {
            ushort length = Convert.ToUInt16(ReadShort(stream));
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = (char)stream.ReadByte();
            }
            return new string(chars);
        }

        private void WriteString(string s, MemoryStream stream)
        {
            WriteShort(Convert.ToInt16((ushort)s.Length), stream);
            for (int i = 0; i < s.Length; i++)
            {
                stream.WriteByte((byte)s[i]);
            }
        }

        private byte[] ReadByteArray(MemoryStream stream)
        {
            int length = ReadInt(stream);
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = (byte)stream.ReadByte();
            }
            return data;
        }

        private void WriteByteArray(byte[] data, MemoryStream stream)
        {
            WriteInt(data.Length, stream);
            for (int i = 0; i < data.Length; i++)
            {
                stream.WriteByte(data[i]);
            }
        }

        private NBTCompound ReadCompound(MemoryStream stream)
        {
            NBTCompound compound = new NBTCompound();
            NBTTag lastTag;
            while((lastTag = ReadTag(stream)).Tag != 0)
            {
                compound.AddTag(lastTag);
            }
            return compound;
        }

        private void WriteCompound(NBTCompound compound, MemoryStream stream)
        {
            foreach(NBTTag tag in compound.Tags)
            {
                WriteTag(tag, stream);
            }
            stream.WriteByte(0);
        }

        private NBTTag ReadTag(MemoryStream stream)
        {
            byte tag = (byte)stream.ReadByte();
            if(tag == 255)
            {
                return new NBTTag("END", 0, null);
            }
            string name = tag == 0 ? "END" : ReadString(stream);
            switch (tag)
            {
                case 1: //byte tag
                    return new NBTTag(name, tag, (byte)stream.ReadByte());
                case 2:
                    return new NBTTag(name, tag, ReadShort(stream));
                case 3:
                    return new NBTTag(name, tag, ReadInt(stream));
                case 4:
                    return new NBTTag(name, tag, ReadLong(stream));
                case 7:
                    return new NBTTag(name, tag, ReadByteArray(stream));
                case 8:
                    return new NBTTag(name, tag, ReadString(stream));
                case 10:
                    return new NBTTag(name, tag, ReadCompound(stream));
                case 0:
                    return new NBTTag(name, 0, null);
                default:
                    throw new NotImplementedException("Tag read not implemented."); //not all NBT tag types are implemented.
            }
        }

        private void WriteTag(NBTTag tag, MemoryStream stream)
        {
            stream.WriteByte(tag.Tag);
            WriteString(tag.Name, stream);
            switch (tag.Tag)
            {
                case 1:
                    stream.WriteByte((byte)tag.Value); break;
                case 2:
                    WriteShort((short)tag.Value, stream); break;
                case 3:
                    WriteInt((int)tag.Value, stream); break;
                case 4:
                    WriteLong((long)tag.Value, stream); break;
                case 7:
                    WriteByteArray((byte[])tag.Value, stream); break;
                case 8:
                    WriteString((string)tag.Value, stream); break;
                case 10:
                    WriteCompound((NBTCompound)tag.Value, stream); break;
                default:
                    throw new NotImplementedException("Tag write not implemented."); //not all NBT tag types are implemented.
            }
        }
    }
}
