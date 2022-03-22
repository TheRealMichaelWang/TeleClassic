using System.Net.Sockets;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class SetTextHotkeyPacket : Packet
    {
        public static byte KeyModNone = 0;
        public static byte KeyModCtrl = 1;
        public static byte KeyModShift = 2;
        public static byte KyeModAlt = 4;

        public readonly string Label;
        public readonly string Action;
        public readonly int KeyCode; //use these specifications (LWJGL) from: https://gist.github.com/Mumfrey/5cfc3b7e14fef91b6fa56470dc05218a
        public readonly byte KeyMods;

        public SetTextHotkeyPacket(string label, string action, int keyCode, byte keyMods) : base(0x15)
        {
            this.Label = label;
            this.Action = action;
            this.KeyCode = keyCode;
            this.KeyMods = keyMods;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x15);
            writer.WriteString(this.Label);
            writer.WriteString(this.Action);
            writer.WriteInt(this.KeyCode);
            writer.WriteByte(this.KeyMods);
        }
    }
}
