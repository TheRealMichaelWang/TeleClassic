using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace TeleClassic
{
    public sealed class NBT
    {
        public static NBTObject ReadObject(BinaryReader reader)
        {
            byte tag_id = reader.ReadByte();
            switch (tag_id)
            {
                case 0:
                    return null;
                case 1:
                    return new NBTByte(reader);
                case 2:
                    return new NBTShort(reader);
                case 3:
                    return new NBTInt(reader);
                case 4:
                    return new NBTLong(reader);
                case 5:
                    return new NBTFloat(reader);
                case 7:
                    return new NBTByteArray(reader);
                case 8:
                    return new NBTString(reader);
                case 10:
                    return new NBTCompound(reader);
                default:
                    throw new NotImplementedException();
            }
        }

        private readonly NBTCompound Head;
        private string filePath;

        public NBT(string filePath)
        {
            this.filePath = filePath;
            if (File.Exists(filePath))
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Decompress))
                using (BinaryReader reader = new BinaryReader(gZip))
                {
                    if (reader.ReadByte() != 10)
                        throw new InvalidOperationException();
                    Head = new NBTCompound(reader);
                }
            }
            else
            {
                File.Create(filePath).Close();
                Head = new NBTCompound("ClassicWorld", new List<NBTObject>());
            }
        }

        public void Save()
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            using (GZipStream gZip = new GZipStream(fileStream, CompressionMode.Compress))
            using (BinaryWriter writer = new BinaryWriter(gZip))
            {
                Head.WriteBack(writer);
            }
        }

        public object FindObject(string path)
        {
            NBTObject currentObj = Head;
            if (path == string.Empty)
                return Head;
            foreach (string objPath in path.Split('.'))
                currentObj = currentObj.FindChild(objPath);
            return currentObj.GetPayload();
        }

        public bool ObjectExists(string path)
        {
            try
            {
                FindObject(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SetObject(string parentPath, NBTObject nbtObject)
        {
            NBTCompound parent = (NBTCompound)FindObject(parentPath);
            if (parent.HasChild(nbtObject.Name))
                parent.SetObject(nbtObject);
            else
                parent.AddChild(nbtObject);
        }
    }

    public abstract class NBTObject
    {
        public readonly string Name;
        public readonly byte Tag;

        public NBTObject(string name, byte tag)
        {
            Name = name;
            Tag = tag;
        }

        public NBTObject(BinaryReader reader, byte tag)
        {
            Tag = tag;
            int name_length = IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
            char[] buffer = new char[name_length];
            for (int i = 0; i < name_length; i++)
                buffer[i] = reader.ReadChar();
            Name = new string(buffer);
        }

        public abstract object GetPayload();

        public virtual NBTObject FindChild(string name) => throw new InvalidOperationException();

        public virtual void WriteBack(BinaryWriter writer)
        {
            writer.Write(Tag);
            writer.Write((ushort)IPAddress.HostToNetworkOrder((short)Name.Length));
            foreach (char c in Name)
                writer.Write((byte)c);
        }
    }

    public sealed class NBTByte : NBTObject
    {
        public readonly byte Byte;

        public NBTByte(string name, byte b) : base(name, 1)
        {
            Byte = b;
        }

        public NBTByte(BinaryReader reader) : base(reader, 1)
        {
            Byte = reader.ReadByte();
        }

        public override object GetPayload() => Byte;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write(Byte);
        }
    }

    public sealed class NBTShort : NBTObject
    {
        public readonly short Short;

        public NBTShort(string name, short s) : base(name, 2)
        {
            Short = s;
        }

        public NBTShort(BinaryReader reader) : base(reader, 2)
        {
            Short = IPAddress.NetworkToHostOrder(reader.ReadInt16());
        }

        public override object GetPayload() => Short;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write(IPAddress.HostToNetworkOrder(Short));
        }
    }

    public sealed class NBTInt : NBTObject
    {
        public readonly int Int;

        public NBTInt(string name, int i) : base(name, 5)
        {
            Int = i;
        }

        public NBTInt(BinaryReader reader) : base(reader, 5)
        {
            Int = IPAddress.NetworkToHostOrder(reader.ReadInt32());
        }

        public override object GetPayload() => Int;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write(IPAddress.HostToNetworkOrder(this.Int));
        }
    }

    public sealed class NBTLong : NBTObject
    {
        public readonly long Long;

        public NBTLong(string name, long l) : base(name, 4)
        {
            Long = l;
        }

        public NBTLong(BinaryReader reader) : base(reader, 4)
        {
            Long = IPAddress.NetworkToHostOrder(reader.ReadInt64());
        }

        public override object GetPayload() => Long;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write(IPAddress.HostToNetworkOrder(Long));
        }
    }

    public sealed class NBTFloat : NBTObject
    {
        public readonly float Float;

        public NBTFloat(string name, float f) : base(name, 3)
        {
            this.Float = f;
        }

        public NBTFloat(BinaryReader reader) : base(reader, 3)
        {
            this.Float =  reader.ReadSingle();
        }

        public override object GetPayload() => Float;
    }

    public sealed class NBTString : NBTObject
    {
        public readonly string String;

        public NBTString(string name, string str) : base(name, 8)
        {
            String = str;
        }

        public NBTString(BinaryReader reader) : base(reader, 8)
        {
            int length = IPAddress.NetworkToHostOrder((short)reader.ReadUInt16());
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
                buffer[i] = reader.ReadChar();
            String = new string(buffer);
        }

        public override object GetPayload() => String;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write((ushort)IPAddress.HostToNetworkOrder((short)String.Length));
            foreach (char c in String)
                writer.Write(c);
        }
    }

    public sealed class NBTByteArray : NBTObject
    {
        public readonly byte[] Data;

        public NBTByteArray(string name, byte[] data) : base(name, 7)
        {
            Data = data;
        }

        public NBTByteArray(BinaryReader reader) : base(reader, 7)
        {
            int length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            Data = new byte[length];
            for (int i = 0; i < length; i++)
                Data[i] = reader.ReadByte();
        }

        public override object GetPayload() => Data;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            writer.Write(IPAddress.HostToNetworkOrder(Data.Length));
            foreach (byte b in Data)
                writer.Write(b);
        }
    }

    public sealed class NBTCompound : NBTObject
    {
        public List<NBTObject> Children;
        private readonly Dictionary<string, NBTObject> childrenLookup;

        public NBTCompound(string name, List<NBTObject> children) : base(name, 10)
        {
            Children = children;
            childrenLookup = new Dictionary<string, NBTObject>();
            foreach (NBTObject child in Children)
                childrenLookup[child.Name] = child;
        }

        public NBTCompound(BinaryReader reader) : base(reader, 10)
        {
            Children = new List<NBTObject>();
            childrenLookup = new Dictionary<string, NBTObject>();

            while (true)
            {
                NBTObject readObject = NBT.ReadObject(reader);
                if (readObject != null)
                    AddChild(readObject);
                else
                    break;
            }
        }

        public void AddChild(NBTObject nbtObject)
        {
            Children.Add(nbtObject);
            childrenLookup[nbtObject.Name] = nbtObject;
        }

        public override object GetPayload() => this;//.Children;

        public override void WriteBack(BinaryWriter writer)
        {
            base.WriteBack(writer);
            foreach (NBTObject child in Children)
                child.WriteBack(writer);
            writer.Write((byte)0);
        }

        public bool HasChild(string name) => childrenLookup.ContainsKey(name);

        public void SetObject(NBTObject newObject)
        {
            Children.Remove(childrenLookup[newObject.Name]);
            Children.Add(newObject);
            childrenLookup[newObject.Name] = newObject;
        }

        public override NBTObject FindChild(string name) => childrenLookup[name];
    }
}