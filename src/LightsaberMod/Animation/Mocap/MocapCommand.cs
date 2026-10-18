using Jotunn.Entities;

namespace LightsaberMod.Animation.Mocap
{
    /// <summary><c>lightsaber_mocap</c>: reload mocap/lightsaber_mocap.txt and its BVH files without restarting.</summary>
    internal sealed class MocapCommand : ConsoleCommand
    {
        public override string Name => "lightsaber_mocap";

        public override string Help => "Reload lightsaber motion-capture clips (mocap/lightsaber_mocap.txt)";

        public override void Run(string[] args)
        {
            Console.instance.Print(MocapLibrary.Reload());
            Console.instance.Print("Per-line problems, if any, are in BepInEx/LogOutput.log.");
        }
    }
}
