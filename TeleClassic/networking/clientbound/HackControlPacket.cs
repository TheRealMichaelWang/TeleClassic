using System.Net.Sockets;
using TeleClassic.Networking;
using TeleClassic.Networking.Clientbound;

namespace TeleClassic.Networking.Clientbound
{
    public sealed class HackControlPacket : Packet
    {
        public readonly bool Flying;
        public readonly bool NoClip;
        public readonly bool Speeding;
        public readonly bool SpawnControl;
        public readonly bool ThirdPersonView;
        public readonly short JumpHeight;

        public HackControlPacket(bool flying, bool noclip, bool speeding, bool spawnControl, bool thirdPersonView, short jumpHeight) : base(0x20)
        {
            this.Flying = flying;
            this.NoClip = noclip;
            this.Speeding = speeding;
            this.SpawnControl = spawnControl;
            this.ThirdPersonView = thirdPersonView;
            this.JumpHeight = jumpHeight;
        }

        public override void Send(NetworkStream stream)
        {
            MinecraftStreamWriter writer = new MinecraftStreamWriter(stream);
            writer.WriteByte(0x20);
            writer.WriteByte(this.Flying ? (byte)1 : (byte)0);
            writer.WriteByte(this.NoClip ? (byte)1 : (byte)0);
            writer.WriteByte(this.Speeding ? (byte)1 : (byte)0);
            writer.WriteByte(this.SpawnControl ? (byte)1 : (byte)0);
            writer.WriteByte(this.ThirdPersonView ? (byte)1 : (byte)0);
            writer.WriteShort(this.JumpHeight);
        }
    }
}

namespace TeleClassic.Networking
{
    public partial class PlayerSession
    {
        public void ResetHackControl()
        {
            if (this.ExtensionManager.SupportsExtension("HackControl"))
                this.SendPacket(new HackControlPacket(false, false, false, false, false, 60));
        }
    }
}
