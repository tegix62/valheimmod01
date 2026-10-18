using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LightsaberMod.Animation.Mocap
{
    /// <summary>One joint of a BVH skeleton.</summary>
    internal sealed class BvhJoint
    {
        public string Name;
        public int Parent = -1;
        public Vector3 Offset;           // BVH (right-handed) units, unconverted
        public string[] Channels = new string[0];
        public int ChannelStart;         // index of this joint's first value in a frame
    }

    /// <summary>
    /// A parsed BVH motion-capture file: the joint hierarchy and every frame's channel values.
    /// End Sites are skipped; they carry no motion.
    /// </summary>
    internal sealed class BvhClip
    {
        public readonly List<BvhJoint> Joints = new List<BvhJoint>();
        public float FrameTime;
        public float[][] Frames;

        public int FrameCount => Frames.Length;
        public float Length => Mathf.Max(0f, (FrameCount - 1) * FrameTime);

        public static BvhClip Parse(string text)
        {
            var tokens = new Tokens(text);
            var clip = new BvhClip();
            int channelCount = 0;

            tokens.Expect("HIERARCHY");
            var stack = new Stack<int>();
            int current = -1;

            while (true)
            {
                string t = tokens.Next();
                if (t == "ROOT" || t == "JOINT")
                {
                    var joint = new BvhJoint { Name = tokens.Next(), Parent = current };
                    clip.Joints.Add(joint);
                    tokens.Expect("{");
                    stack.Push(current);
                    current = clip.Joints.Count - 1;
                }
                else if (t == "End")
                {
                    tokens.Expect("Site");
                    tokens.Expect("{");
                    tokens.Expect("OFFSET");
                    tokens.Float(); tokens.Float(); tokens.Float();
                    tokens.Expect("}");
                }
                else if (t == "OFFSET")
                {
                    clip.Joints[current].Offset = new Vector3(tokens.Float(), tokens.Float(), tokens.Float());
                }
                else if (t == "CHANNELS")
                {
                    int n = tokens.Int();
                    var channels = new string[n];
                    for (int i = 0; i < n; i++) channels[i] = tokens.Next();
                    clip.Joints[current].Channels = channels;
                    clip.Joints[current].ChannelStart = channelCount;
                    channelCount += n;
                }
                else if (t == "}")
                {
                    current = stack.Pop();
                }
                else if (t == "MOTION")
                {
                    break;
                }
                else
                {
                    throw new FormatException($"Unexpected '{t}' in BVH hierarchy");
                }
            }

            if (clip.Joints.Count == 0) throw new FormatException("BVH has no joints");

            tokens.Expect("Frames:");
            int frames = tokens.Int();
            tokens.Expect("Frame");
            tokens.Expect("Time:");
            clip.FrameTime = tokens.Float();
            if (frames <= 0 || clip.FrameTime <= 0f) throw new FormatException("BVH has no frames");

            clip.Frames = new float[frames][];
            for (int f = 0; f < frames; f++)
            {
                var values = new float[channelCount];
                for (int c = 0; c < channelCount; c++) values[c] = tokens.Float();
                clip.Frames[f] = values;
            }
            return clip;
        }

        /// <summary>
        /// A joint's local position and rotation at one frame, converted from BVH's right-handed
        /// Y-up space into Unity's left-handed one by mirroring the X axis.
        /// </summary>
        public void Local(int joint, int frame, out Vector3 position, out Quaternion rotation)
        {
            BvhJoint j = Joints[joint];
            float[] values = Frames[Mathf.Clamp(frame, 0, FrameCount - 1)];
            Vector3 p = j.Offset;
            bool hasPosition = false;
            Quaternion q = Quaternion.identity;

            for (int i = 0; i < j.Channels.Length; i++)
            {
                float v = values[j.ChannelStart + i];
                switch (j.Channels[i].ToLowerInvariant())
                {
                    case "xposition": if (!hasPosition) { p = Vector3.zero; hasPosition = true; } p.x = v; break;
                    case "yposition": if (!hasPosition) { p = Vector3.zero; hasPosition = true; } p.y = v; break;
                    case "zposition": if (!hasPosition) { p = Vector3.zero; hasPosition = true; } p.z = v; break;
                    // Mirroring X keeps rotations about X and flips those about Y and Z.
                    case "xrotation": q *= Quaternion.AngleAxis(v, Vector3.right); break;
                    case "yrotation": q *= Quaternion.AngleAxis(-v, Vector3.up); break;
                    case "zrotation": q *= Quaternion.AngleAxis(-v, Vector3.forward); break;
                }
            }

            position = new Vector3(-p.x, p.y, p.z);
            rotation = q;
        }

        public static Vector3 ConvertOffset(Vector3 bvh) => new Vector3(-bvh.x, bvh.y, bvh.z);

        private sealed class Tokens
        {
            private readonly string[] _items;
            private int _index;

            public Tokens(string text)
            {
                _items = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }

            public string Next()
            {
                if (_index >= _items.Length) throw new FormatException("BVH ended unexpectedly");
                return _items[_index++];
            }

            public void Expect(string word)
            {
                string t = Next();
                if (!string.Equals(t, word, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"Expected '{word}' but found '{t}'");
            }

            public float Float() => float.Parse(Next(), NumberStyles.Float, CultureInfo.InvariantCulture);

            public int Int() => int.Parse(Next(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}
