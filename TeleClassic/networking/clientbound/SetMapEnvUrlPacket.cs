using System.Net.Sockets;
using TeleClassic.Gameplay;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class SetMapEnvUrlPacket : Packet
    {
        public readonly string TexturePackURL;

        public SetMapEnvUrlPacket(string texturePackUrl) : base(0x28)
        {
            this.TexturePackURL = texturePackUrl;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x28);
            writer.WriteString(this.TexturePackURL);
        }
    }

    public sealed class SetMapEnvPropertyPacket : Packet
    {
        public enum PropertyType : byte
        {
            SideBlockType = 0,
            EdgeBlockType  = 1,
            MapEdgeHeight = 2,
            MapCloudsHeight = 3,
            MapFogViewDistance = 4,
            CloudSpeed = 5,
            WeatherSpeed = 6,
            WeatherFade = 7,
            UseExponentialFog=8,
            MapEdgeHeightOffset = 9
        }

        public readonly PropertyType Property;
        public readonly int PropertyValue;

        public SetMapEnvPropertyPacket(PropertyType property, int propertyValue) : base(0x29)
        {
            this.Property = property;
            this.PropertyValue = propertyValue;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x29);
            writer.WriteByte((byte)this.Property);
            writer.WriteInt(this.PropertyValue);
        }
    }

    public sealed class EnvSetWeatherTypePacket : Packet
    {
        public World.EnvironmentConfiguration.WeatherType WeatherType;
        
        public EnvSetWeatherTypePacket(World.EnvironmentConfiguration.WeatherType weatherType) : base(0x1F)
        {
            this.WeatherType = weatherType;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x1F);
            writer.WriteByte((byte)this.WeatherType);
        }
    }
}
