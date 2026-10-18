using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LightsaberMod.Animation.Mocap
{
    /// <summary>
    /// A hidden skeleton built from a BVH clip, with a humanoid Avatar created at runtime so
    /// Unity can read its pose as muscle values and retarget them onto Valheim's character.
    /// </summary>
    internal sealed class MocapRig
    {
        // Normalised (lowercase, letters/digits only, "mixamorig" removed) joint names used by
        // common mocap exports: Mixamo, Rokoko, CMU, 3ds Max Biped, Plask, Move.ai.
        private static readonly Dictionary<HumanBodyBones, string[]> Aliases = new Dictionary<HumanBodyBones, string[]>
        {
            [HumanBodyBones.Hips] = new[] { "hips", "pelvis", "hip" },
            [HumanBodyBones.Spine] = new[] { "spine", "spine1", "lowerback", "abdomen", "spine01" },
            [HumanBodyBones.Chest] = new[] { "spine1", "spine2", "chest", "spine02", "spine3" },
            [HumanBodyBones.UpperChest] = new[] { "spine2", "spine3", "upperchest", "spine03", "spine4" },
            [HumanBodyBones.Neck] = new[] { "neck", "neck1", "neck01" },
            [HumanBodyBones.Head] = new[] { "head" },
            [HumanBodyBones.LeftShoulder] = new[] { "leftshoulder", "lcollar", "leftcollar", "lclavicle", "leftclavicle", "shoulderl" },
            [HumanBodyBones.LeftUpperArm] = new[] { "leftarm", "leftupperarm", "lshldr", "luparm", "lupperarm", "upperarml" },
            [HumanBodyBones.LeftLowerArm] = new[] { "leftforearm", "leftlowerarm", "lforearm", "llowerarm", "forearml", "lowerarml" },
            [HumanBodyBones.LeftHand] = new[] { "lefthand", "lhand", "handl" },
            [HumanBodyBones.RightShoulder] = new[] { "rightshoulder", "rcollar", "rightcollar", "rclavicle", "rightclavicle", "shoulderr" },
            [HumanBodyBones.RightUpperArm] = new[] { "rightarm", "rightupperarm", "rshldr", "ruparm", "rupperarm", "upperarmr" },
            [HumanBodyBones.RightLowerArm] = new[] { "rightforearm", "rightlowerarm", "rforearm", "rlowerarm", "forearmr", "lowerarmr" },
            [HumanBodyBones.RightHand] = new[] { "righthand", "rhand", "handr" },
            [HumanBodyBones.LeftUpperLeg] = new[] { "leftupleg", "leftupperleg", "lthigh", "leftthigh", "lhip", "thighl", "upperlegl" },
            [HumanBodyBones.LeftLowerLeg] = new[] { "leftleg", "leftlowerleg", "lshin", "leftshin", "lknee", "calfl", "lowerlegl" },
            [HumanBodyBones.LeftFoot] = new[] { "leftfoot", "lfoot", "footl" },
            [HumanBodyBones.LeftToes] = new[] { "lefttoebase", "lefttoe", "ltoe", "toel" },
            [HumanBodyBones.RightUpperLeg] = new[] { "rightupleg", "rightupperleg", "rthigh", "rightthigh", "rhip", "thighr", "upperlegr" },
            [HumanBodyBones.RightLowerLeg] = new[] { "rightleg", "rightlowerleg", "rshin", "rightshin", "rknee", "calfr", "lowerlegr" },
            [HumanBodyBones.RightFoot] = new[] { "rightfoot", "rfoot", "footr" },
            [HumanBodyBones.RightToes] = new[] { "righttoebase", "righttoe", "rtoe", "toer" },
        };

        // Unity refuses to build a humanoid without these.
        private static readonly HumanBodyBones[] Required =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
        };

        private static Transform _container;

        public readonly BvhClip Clip;
        private readonly Transform _root;
        private readonly Transform[] _bones;
        private readonly HumanPoseHandler _handler;
        private readonly float _scale;

        private MocapRig(BvhClip clip, Transform root, Transform[] bones, HumanPoseHandler handler, float scale)
        {
            Clip = clip;
            _root = root;
            _bones = bones;
            _handler = handler;
            _scale = scale;
        }

        public static MocapRig Build(BvhClip clip, string name, out string error)
        {
            error = null;
            if (_container == null)
            {
                var holder = new GameObject("LightsaberMod_MocapRigs");
                // Bare transforms only: nothing renders or updates, so leaving it active costs nothing.
                Object.DontDestroyOnLoad(holder);
                _container = holder.transform;
            }

            // Many exports are in centimetres; Avatar setup is happier near real-world scale.
            float hipHeight = Mathf.Abs(clip.Joints[0].Offset.y) + clip.Joints.Where(j => j.Parent == 0).Select(j => Mathf.Abs(j.Offset.y)).DefaultIfEmpty(0f).Max();
            float scale = hipHeight > 10f ? 0.01f : 1f;

            var root = new GameObject("mocap_" + name).transform;
            root.SetParent(_container, false);
            var bones = new Transform[clip.Joints.Count];
            for (int i = 0; i < clip.Joints.Count; i++)
            {
                BvhJoint j = clip.Joints[i];
                var t = new GameObject(j.Name).transform;
                t.SetParent(j.Parent < 0 ? root : bones[j.Parent], false);
                t.localPosition = BvhClip.ConvertOffset(j.Offset) * scale;
                t.localRotation = Quaternion.identity; // the BVH rest pose: expected to be a T-pose
                bones[i] = t;
            }

            var human = new List<HumanBone>();
            var used = new HashSet<int>();
            foreach (var alias in Aliases)
            {
                int index = FindJoint(clip, alias.Value, used);
                if (index < 0) continue;
                used.Add(index);
                human.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)alias.Key],
                    boneName = clip.Joints[index].Name,
                    limit = new HumanLimit { useDefaultValues = true },
                });
            }

            string[] missing = Required
                .Where(b => !human.Any(h => h.humanName == HumanTrait.BoneName[(int)b]))
                .Select(b => b.ToString())
                .ToArray();
            if (missing.Length > 0)
            {
                Object.Destroy(root.gameObject);
                error = "couldn't identify joints for " + string.Join(", ", missing) +
                        ". Joint names in the file: " + string.Join(", ", clip.Joints.Select(j => j.Name).Take(40));
                return null;
            }

            var skeleton = new List<SkeletonBone> { Skeleton(root) };
            skeleton.AddRange(bones.Select(Skeleton));

            var description = new HumanDescription
            {
                human = human.ToArray(),
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f,
                feetSpacing = 0f, hasTranslationDoF = false,
            };

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(root.gameObject, description);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Object.Destroy(root.gameObject);
                error = "Unity couldn't build a humanoid avatar from this skeleton (is its rest pose a T-pose?)";
                return null;
            }

            return new MocapRig(clip, root, bones, new HumanPoseHandler(avatar, root), scale);
        }

        private static int FindJoint(BvhClip clip, string[] names, HashSet<int> used)
        {
            foreach (string wanted in names)
                for (int i = 0; i < clip.Joints.Count; i++)
                    if (!used.Contains(i) && Normalise(clip.Joints[i].Name) == wanted)
                        return i;
            return -1;
        }

        private static string Normalise(string name)
        {
            string n = new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            return n.StartsWith("mixamorig") ? n.Substring("mixamorig".Length) : n;
        }

        private static SkeletonBone Skeleton(Transform t) => new SkeletonBone
        {
            name = t.name,
            position = t.localPosition,
            rotation = t.localRotation,
            scale = t.localScale,
        };

        /// <summary>Poses the hidden skeleton at a time in seconds and reads it back as muscles.</summary>
        public void Sample(float time, ref HumanPose pose)
        {
            float frame = Mathf.Clamp(time / Clip.FrameTime, 0f, Clip.FrameCount - 1);
            int f0 = Mathf.FloorToInt(frame);
            int f1 = Mathf.Min(f0 + 1, Clip.FrameCount - 1);
            float k = frame - f0;

            for (int i = 0; i < _bones.Length; i++)
            {
                Clip.Local(i, f0, out Vector3 p0, out Quaternion r0);
                Clip.Local(i, f1, out Vector3 p1, out Quaternion r1);
                _bones[i].localPosition = Vector3.Lerp(p0, p1, k) * _scale;
                _bones[i].localRotation = Quaternion.Slerp(r0, r1, k);
            }
            _handler.GetHumanPose(ref pose);
        }
    }
}
