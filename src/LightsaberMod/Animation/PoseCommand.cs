using System;
using System.Globalization;
using System.Linq;
using Jotunn.Entities;
using UnityEngine;

namespace LightsaberMod.Animation
{
    /// <summary>
    /// <c>lightsaber_pose</c>: view and tune the stance poses live, then save them.
    /// <code>
    /// lightsaber_pose                          list forms and bones
    /// lightsaber_pose makashi                  show the Makashi pose
    /// lightsaber_pose makashi RightUpperArm -30 0 5   set one bone (applies immediately)
    /// lightsaber_pose save                     write lightsaber_poses.txt
    /// </code>
    /// </summary>
    internal sealed class PoseCommand : ConsoleCommand
    {
        public override string Name => "lightsaber_pose";

        public override string Help => "Tune lightsaber stance poses: [form] [Bone x y z] | save";

        public override void Run(string[] args)
        {
            if (args.Length == 0)
            {
                Print("Forms: " + string.Join(", ", PoseLibrary.Keys.OrderBy(k => k)));
                Print("Bones: " + string.Join(", ", PoseLibrary.Bones.Select(b => b.ToString())));
                Print("Draw a saber in that form and stand still to see the pose. Values are degrees on the bone's own axes.");
                return;
            }

            if (args[0].Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                PoseLibrary.Save();
                Print($"Saved to {PoseLibrary.FileName} next to LightsaberMod.dll.");
                return;
            }

            string key = args[0].ToLowerInvariant();
            if (args.Length == 1)
            {
                var pose = PoseLibrary.Get(key);
                if (pose == null) { Print($"No pose '{key}'."); return; }
                foreach (var bone in pose.OrderBy(b => b.Key.ToString()))
                    Print(string.Format(CultureInfo.InvariantCulture, "{0}.{1} = {2}, {3}, {4}", key, bone.Key, bone.Value.x, bone.Value.y, bone.Value.z));
                return;
            }

            if (args.Length != 5 || !Enum.TryParse(args[1], true, out HumanBodyBones boneId)
                || !PoseLibrary.TryParseVector(new[] { args[2], args[3], args[4] }, out Vector3 euler))
            {
                Print("Usage: lightsaber_pose <form> <Bone> <x> <y> <z>");
                return;
            }

            PoseLibrary.Set(key, boneId, euler);
            Print($"{key}.{boneId} set. Use 'lightsaber_pose save' to keep it.");
        }

        private static void Print(string text) => Console.instance.Print(text);
    }
}
