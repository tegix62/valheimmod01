using System.Linq;
using Jotunn.Entities;

namespace LightsaberMod.Animation
{
    /// <summary>
    /// <c>lightsaber_clips [filter]</c>: prints the animation clip names in the player's
    /// animator, for writing lightsaber_animations.txt rules.
    /// </summary>
    internal sealed class ListClipsCommand : ConsoleCommand
    {
        public override string Name => "lightsaber_clips";

        public override string Help => "List the player animator's clip names (optionally filtered), for lightsaber_animations.txt";

        public override void Run(string[] args)
        {
            Player player = Player.m_localPlayer;
            UnityEngine.Animator animator = player != null ? Util.Compat.GetField<UnityEngine.Animator>(player, "m_animator") : null;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Console.instance.Print("No player animator yet; load into a world first.");
                return;
            }

            string filter = args.Length > 0 ? args[0].ToLowerInvariant() : null;
            string[] names = animator.runtimeAnimatorController.animationClips
                .Where(c => c != null)
                .Select(c => c.name)
                .Distinct()
                .Where(n => filter == null || n.ToLowerInvariant().Contains(filter))
                .OrderBy(n => n)
                .ToArray();

            foreach (string name in names)
                Console.instance.Print(name);
            Console.instance.Print($"{names.Length} clip(s). Their animation events are listed in the Unity editor; copy them to replacements.");
        }
    }
}
