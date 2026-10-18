using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LightsaberMod.Forms;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Animation.Mocap
{
    /// <summary>One motion-capture clip assigned to a form's attack slot.</summary>
    internal sealed class MocapEntry
    {
        public string File;
        public MocapRig Rig;
        public float Start;
        public float End;
        public float Speed = 1f;
        public bool FullBody;

        public float Duration => (End - Start) / Speed;
    }

    /// <summary>
    /// Loads <c>mocap/lightsaber_mocap.txt</c> and the BVH files it names. Each line assigns
    /// clips to a form's slot:
    /// <code>
    /// # key.slot = file.bvh [, file2.bvh ...] [speed=1.2] [start=0.1] [end=0.9] [body=full]
    /// makashi.primary = lunge1.bvh, lunge2.bvh, lunge3.bvh   # one per chain step
    /// makashi.secondary = riposte.bvh speed=1.1
    /// soresu.idle = guard_loop.bvh                          # looped while standing ready
    /// *.secondary = generic_spin.bvh body=full              # fallback for every form
    /// </code>
    /// Keys are form ids, "jarkai", or "*". Slots are primary, secondary and idle.
    /// </summary>
    internal static class MocapLibrary
    {
        public const string FolderName = "mocap";
        public const string MappingName = "lightsaber_mocap.txt";
        public static readonly string[] Slots = { "primary", "secondary", "idle" };

        private static readonly Dictionary<string, List<MocapEntry>> Entries = new Dictionary<string, List<MocapEntry>>();
        private static readonly Dictionary<string, MocapRig> Rigs = new Dictionary<string, MocapRig>(StringComparer.OrdinalIgnoreCase);

        public static bool Enabled => Entries.Count > 0 && LightsaberPlugin.UseMocap.Value;

        private static string _folder;

        public static void Load(string pluginDir)
        {
            _folder = Path.Combine(pluginDir, FolderName);
            Reload();
        }

        /// <summary>(Re)reads the mapping and clips. Returns a summary line.</summary>
        public static string Reload()
        {
            Entries.Clear();
            Rigs.Clear();
            string mapping = _folder == null ? null : Path.Combine(_folder, MappingName);
            if (mapping == null || !File.Exists(mapping)) return $"No {FolderName}/{MappingName}; motion capture is off.";

            int lineNo = 0, clips = 0;
            foreach (string raw in File.ReadAllLines(mapping))
            {
                lineNo++;
                string line = raw.Split('#')[0].Trim();
                if (line.Length == 0) continue;

                int eq = line.IndexOf('=');
                int dot = eq > 0 ? line.LastIndexOf('.', eq) : -1;
                if (eq < 0 || dot < 0)
                {
                    Warn(lineNo, "expected 'form.slot = file.bvh ...'");
                    continue;
                }

                string key = line.Substring(0, dot).Trim().ToLowerInvariant();
                string slot = line.Substring(dot + 1, eq - dot - 1).Trim().ToLowerInvariant();
                if (!Slots.Contains(slot)) { Warn(lineNo, $"unknown slot '{slot}' (use primary, secondary or idle)"); continue; }
                if (key != "*" && key != "jarkai" && !SaberForm.All.Any(f => f.Id == key)) { Warn(lineNo, $"unknown form '{key}'"); continue; }

                var options = new Dictionary<string, string>();
                var files = new List<string>();
                foreach (string part in line.Substring(eq + 1).Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int optEq = part.IndexOf('=');
                    if (optEq > 0) options[part.Substring(0, optEq).ToLowerInvariant()] = part.Substring(optEq + 1);
                    else files.Add(part);
                }

                var list = new List<MocapEntry>();
                foreach (string file in files)
                {
                    MocapRig rig = LoadRig(file, lineNo);
                    if (rig == null) continue;
                    var entry = new MocapEntry
                    {
                        File = file, Rig = rig,
                        Start = Option(options, "start", 0f),
                        End = Option(options, "end", rig.Clip.Length),
                        Speed = Math.Max(0.05f, Option(options, "speed", 1f)),
                        FullBody = options.TryGetValue("body", out string body) && body.Equals("full", StringComparison.OrdinalIgnoreCase),
                    };
                    entry.End = Math.Min(Math.Max(entry.End, entry.Start + 0.05f), rig.Clip.Length);
                    list.Add(entry);
                    clips++;
                }
                if (list.Count > 0) Entries[key + "." + slot] = list;
            }

            string summary = $"Motion capture: {clips} clip(s) in {Entries.Count} slot(s).";
            Logger.LogInfo(summary);
            return summary;
        }

        private static MocapRig LoadRig(string file, int lineNo)
        {
            if (Rigs.TryGetValue(file, out MocapRig cached)) return cached;
            string path = Path.Combine(_folder, file);
            if (!File.Exists(path)) { Warn(lineNo, $"file '{file}' not found in {FolderName}/"); return null; }

            try
            {
                BvhClip clip = BvhClip.Parse(File.ReadAllText(path));
                MocapRig rig = MocapRig.Build(clip, Path.GetFileNameWithoutExtension(file), out string error);
                if (rig == null) { Warn(lineNo, $"{file}: {error}"); return null; }
                Rigs[file] = rig;
                return rig;
            }
            catch (Exception e)
            {
                Warn(lineNo, $"{file}: {e.Message}");
                return null;
            }
        }

        /// <summary>The clip for a key and slot, cycling through chain steps; falls back to "*".</summary>
        public static MocapEntry Get(string key, string slot, int step)
        {
            if (!Entries.TryGetValue(key + "." + slot, out var list) && !Entries.TryGetValue("*." + slot, out list))
                return null;
            return list[((step % list.Count) + list.Count) % list.Count];
        }

        private static float Option(Dictionary<string, string> options, string name, float fallback) =>
            options.TryGetValue(name, out string v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : fallback;

        private static void Warn(int lineNo, string message) => Logger.LogWarning($"{MappingName}:{lineNo}: {message}");
    }
}
