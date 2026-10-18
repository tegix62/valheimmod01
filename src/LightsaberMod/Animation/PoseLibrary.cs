using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Animation
{
    /// <summary>
    /// Per-form stance poses: rotation offsets (degrees, in each bone's local space) layered on
    /// top of whatever Valheim's animator produced that frame. Keys are form ids plus "jarkai".
    /// </summary>
    /// <remarks>
    /// The defaults are starting guesses made without seeing Valheim's skeleton, kept small so
    /// a wrong axis looks odd rather than broken. Tune them in game with the
    /// <c>lightsaber_pose</c> command; tuned values are saved to <see cref="FileName"/> next to
    /// the DLL and override these.
    /// </remarks>
    internal static class PoseLibrary
    {
        public const string FileName = "lightsaber_poses.txt";

        public static readonly HumanBodyBones[] Bones =
        {
            HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
        };

        private static readonly Dictionary<string, Dictionary<HumanBodyBones, Vector3>> Poses =
            new Dictionary<string, Dictionary<HumanBodyBones, Vector3>>
            {
                // Shii-Cho: square, open ready stance for sweeping in any direction.
                ["shiicho"] = Pose(B(HumanBodyBones.RightUpperArm, 0, 0, -10), B(HumanBodyBones.Spine, 0, 8, 0)),
                // Makashi: side-on fencer, sword arm extended toward the foe, off arm back.
                ["makashi"] = Pose(B(HumanBodyBones.Spine, 0, -20, 0), B(HumanBodyBones.RightUpperArm, -25, 0, 0),
                                   B(HumanBodyBones.RightLowerArm, 0, 0, -15), B(HumanBodyBones.LeftUpperArm, 20, 0, 10)),
                // Soresu: blade held up across the body, elbow tucked: Obi-Wan's guard.
                ["soresu"] = Pose(B(HumanBodyBones.RightUpperArm, -15, 0, 15), B(HumanBodyBones.RightHand, 0, 0, 30),
                                  B(HumanBodyBones.Spine, 5, 0, 0)),
                // Ataru: crouched forward, coiled to spring.
                ["ataru"] = Pose(B(HumanBodyBones.Spine, 15, 0, 0), B(HumanBodyBones.Chest, 5, 0, 0),
                                 B(HumanBodyBones.RightUpperArm, 10, 0, -10)),
                // Shien / Djem So: high two-handed guard, blade over the shoulder.
                ["shien"] = Pose(B(HumanBodyBones.RightUpperArm, -30, 0, 10), B(HumanBodyBones.LeftUpperArm, -30, 0, -10),
                                 B(HumanBodyBones.LeftLowerArm, 0, 0, 20), B(HumanBodyBones.Spine, -5, 0, 0)),
                // Niman: relaxed, balanced middle guard.
                ["niman"] = Pose(B(HumanBodyBones.RightUpperArm, -10, 0, 0)),
                // Juyo / Vaapad: low, twisted, predatory.
                ["juyo"] = Pose(B(HumanBodyBones.Spine, 20, 15, 0), B(HumanBodyBones.RightUpperArm, 10, 0, -15),
                                B(HumanBodyBones.LeftUpperArm, 10, 0, 15)),
                // Jar'Kai: both blades raised, ready to cross.
                ["jarkai"] = Pose(B(HumanBodyBones.RightUpperArm, -15, 0, 15), B(HumanBodyBones.LeftUpperArm, -15, 0, -15),
                                  B(HumanBodyBones.RightHand, 0, 0, 20), B(HumanBodyBones.LeftHand, 0, 0, -20)),
            };

        private static string _path;

        public static IEnumerable<string> Keys => Poses.Keys;

        public static Dictionary<HumanBodyBones, Vector3> Get(string key) =>
            key != null && Poses.TryGetValue(key, out var pose) ? pose : null;

        public static void Set(string key, HumanBodyBones bone, Vector3 euler)
        {
            if (!Poses.TryGetValue(key, out var pose)) Poses[key] = pose = new Dictionary<HumanBodyBones, Vector3>();
            pose[bone] = euler;
        }

        /// <summary>Loads tuned values over the defaults. Format: <c>key.Bone = x, y, z</c>.</summary>
        public static void Load(string pluginDir)
        {
            _path = Path.Combine(pluginDir, FileName);
            if (!File.Exists(_path)) return;

            int lineNo = 0;
            foreach (string raw in File.ReadAllLines(_path))
            {
                lineNo++;
                string line = raw.Split('#')[0].Trim();
                if (line.Length == 0) continue;
                int dot = line.IndexOf('.'), eq = line.IndexOf('=');
                string[] xyz = eq > 0 ? line.Substring(eq + 1).Split(',') : new string[0];
                if (dot < 0 || eq < dot || xyz.Length != 3
                    || !Enum.TryParse(line.Substring(dot + 1, eq - dot - 1).Trim(), true, out HumanBodyBones bone)
                    || !TryParseVector(xyz, out Vector3 v))
                {
                    Logger.LogWarning($"{FileName}:{lineNo}: expected 'form.Bone = x, y, z'");
                    continue;
                }
                Set(line.Substring(0, dot).Trim().ToLowerInvariant(), bone, v);
            }
        }

        public static void Save()
        {
            if (_path == null) return;
            var lines = new List<string>
            {
                "# Lightsaber stance poses: form.Bone = x, y, z (degrees, bone-local, added on top of the game's animation).",
                "# Written by the lightsaber_pose console command; safe to edit by hand.",
            };
            foreach (var pose in Poses.OrderBy(p => p.Key))
                foreach (var bone in pose.Value.OrderBy(b => b.Key.ToString()))
                    lines.Add(string.Format(CultureInfo.InvariantCulture, "{0}.{1} = {2}, {3}, {4}",
                        pose.Key, bone.Key, bone.Value.x, bone.Value.y, bone.Value.z));
            File.WriteAllLines(_path, lines);
        }

        public static bool TryParseVector(string[] parts, out Vector3 v)
        {
            v = Vector3.zero;
            if (parts.Length != 3) return false;
            float[] f = new float[3];
            for (int i = 0; i < 3; i++)
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f[i]))
                    return false;
            v = new Vector3(f[0], f[1], f[2]);
            return true;
        }

        private readonly struct BoneOffset
        {
            public readonly HumanBodyBones Bone;
            public readonly Vector3 Euler;
            public BoneOffset(HumanBodyBones bone, Vector3 euler) { Bone = bone; Euler = euler; }
        }

        private static BoneOffset B(HumanBodyBones bone, float x, float y, float z) => new BoneOffset(bone, new Vector3(x, y, z));

        private static Dictionary<HumanBodyBones, Vector3> Pose(params BoneOffset[] entries) =>
            entries.ToDictionary(e => e.Bone, e => e.Euler);
    }
}
