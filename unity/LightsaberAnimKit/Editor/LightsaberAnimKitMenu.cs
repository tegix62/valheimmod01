using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LightsaberAnimKit
{
    /// <summary>
    /// The Lightsaber menu: validate the map, copy Valheim's animation events onto the
    /// replacement clips, and build the bundle and mapping file the mod loads.
    /// </summary>
    public static class LightsaberAnimKitMenu
    {
        private const string BundleName = "lightsaber_animations";
        private const string MappingName = "lightsaber_animations.txt";
        private const string OutputFolder = "LightsaberBuild";

        // ----- Menu items -----------------------------------------------------------------

        [MenuItem("Lightsaber/1. Name Mixamo Clips After Their Files (selected models)")]
        private static void RenameMixamoClips()
        {
            int renamed = 0;
            foreach (Object o in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(o);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;

                ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                string baseName = Path.GetFileNameWithoutExtension(path).Replace(' ', '_');
                for (int i = 0; i < clips.Length; i++)
                {
                    // Every Mixamo download names its clip "mixamo.com"; the mod maps clips by name, so they must differ.
                    clips[i].name = clips.Length == 1 ? baseName : baseName + "_" + i;
                    renamed++;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            Debug.Log($"[Lightsaber] Renamed {renamed} clip(s). Select the FBX files in the Project window before running this.");
        }

        [MenuItem("Lightsaber/2. Validate Map")]
        private static void ValidateMenu()
        {
            SaberAnimMap map = FindMap();
            if (map != null) Report(Validate(map));
        }

        [MenuItem("Lightsaber/3. Copy Vanilla Events Into Replacements")]
        private static void CopyEventsMenu()
        {
            SaberAnimMap map = FindMap();
            if (map != null) Debug.Log($"[Lightsaber] Copied events into {CopyEvents(map)} clip(s).");
        }

        [MenuItem("Lightsaber/4. Build Bundle + Mapping")]
        private static void BuildMenu()
        {
            SaberAnimMap map = FindMap();
            if (map == null) return;

            List<string> problems = Validate(map);
            if (problems.Any(p => p.StartsWith("ERROR")))
            {
                Report(problems);
                Debug.LogError("[Lightsaber] Fix the errors above, then build again.");
                return;
            }

            CopyEvents(map);
            Build(map);
        }

        // ----- Validation -----------------------------------------------------------------

        private static List<string> Validate(SaberAnimMap map)
        {
            var problems = new List<string>();
            var names = new Dictionary<string, AnimationClip>();

            for (int i = 0; i < map.entries.Count; i++)
            {
                SaberAnimMap.Entry e = map.entries[i];
                string where = $"Entry {i} ({e.form}: {e.VanillaName ?? "?"})";

                if (!SaberAnimMap.Forms.Contains(e.form))
                    problems.Add($"ERROR {where}: unknown form '{e.form}'.");
                if (string.IsNullOrEmpty(e.VanillaName))
                    problems.Add($"ERROR {where}: set vanillaClip (preferred) or vanillaClipName.");
                if (e.replacement == null)
                {
                    problems.Add($"ERROR {where}: no replacement clip.");
                    continue;
                }

                if (e.replacement.name == "mixamo.com")
                    problems.Add($"ERROR {where}: the clip is still called 'mixamo.com'. Select its FBX and run Lightsaber > 1.");
                if (names.TryGetValue(e.replacement.name, out AnimationClip other) && other != e.replacement)
                    problems.Add($"ERROR {where}: another clip is also named '{e.replacement.name}'. Clip names must be unique.");
                names[e.replacement.name] = e.replacement;

                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(e.replacement)) is ModelImporter importer)
                {
                    if (importer.animationType != ModelImporterAnimationType.Human)
                        problems.Add($"ERROR {where}: import its model with Rig > Animation Type = Humanoid, or it won't retarget onto Valheim's character.");
                    ModelImporterClipAnimation info = ClipInfo(importer, e.replacement.name);
                    if (info != null && info.loopTime)
                        problems.Add($"WARN {where}: Loop Time is on; attacks normally shouldn't loop.");
                    if (info != null && (!info.lockRootPositionXZ || !info.lockRootRotation))
                        problems.Add($"WARN {where}: bake root position (XZ) and rotation into pose, or the character will drift from its collider.");
                }

                if (e.vanillaClip == null)
                    problems.Add($"WARN {where}: no vanillaClip, so no events will be copied. Hits won't land unless the clip already has Valheim's events.");
                else if (AnimationUtility.GetAnimationEvents(e.vanillaClip).Length == 0)
                    problems.Add($"WARN {where}: the vanilla clip has no events. Check that the AssetRipper export kept them.");
            }

            if (map.entries.Count == 0) problems.Add("ERROR The map has no entries.");
            return problems;
        }

        private static void Report(List<string> problems)
        {
            if (problems.Count == 0) Debug.Log("[Lightsaber] Map looks good.");
            foreach (string p in problems)
                if (p.StartsWith("ERROR")) Debug.LogError("[Lightsaber] " + p);
                else Debug.LogWarning("[Lightsaber] " + p);
        }

        // ----- Events -----------------------------------------------------------------------

        /// <summary>
        /// Copies each vanilla clip's events onto its replacement, rescaled to the replacement's
        /// length, so hits, chain windows and attack ends fire at the same point in the motion.
        /// </summary>
        private static int CopyEvents(SaberAnimMap map)
        {
            int copied = 0;
            foreach (SaberAnimMap.Entry e in map.entries)
            {
                if (e.vanillaClip == null || e.replacement == null) continue;
                AnimationEvent[] source = AnimationUtility.GetAnimationEvents(e.vanillaClip);
                if (source.Length == 0) continue;

                float scale = e.vanillaClip.length > 0f ? e.replacement.length / e.vanillaClip.length : 1f;
                string path = AssetDatabase.GetAssetPath(e.replacement);

                if (AssetImporter.GetAtPath(path) is ModelImporter importer)
                {
                    // Clips inside an FBX are read-only; their events live in the import settings, in normalised time.
                    ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
                    int i = System.Array.FindIndex(clips, c => c.name == e.replacement.name);
                    if (i < 0) continue;
                    clips[i].events = source.Select(ev => Clone(ev, ev.time / Mathf.Max(0.0001f, e.vanillaClip.length))).ToArray();
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
                else
                {
                    AnimationUtility.SetAnimationEvents(e.replacement, source.Select(ev => Clone(ev, ev.time * scale)).ToArray());
                    EditorUtility.SetDirty(e.replacement);
                }
                copied++;
            }
            AssetDatabase.SaveAssets();
            return copied;
        }

        private static AnimationEvent Clone(AnimationEvent ev, float time) => new AnimationEvent
        {
            time = time,
            functionName = ev.functionName,
            stringParameter = ev.stringParameter,
            floatParameter = ev.floatParameter,
            intParameter = ev.intParameter,
            objectReferenceParameter = ev.objectReferenceParameter,
            messageOptions = ev.messageOptions,
        };

        private static ModelImporterClipAnimation ClipInfo(ModelImporter importer, string clipName)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            return clips.FirstOrDefault(c => c.name == clipName);
        }

        // ----- Build ------------------------------------------------------------------------

        private static void Build(SaberAnimMap map)
        {
            string[] assets = map.entries
                .Where(e => e.replacement != null)
                .Select(e => AssetDatabase.GetAssetPath(e.replacement))
                .Distinct()
                .ToArray();

            Directory.CreateDirectory(OutputFolder);
            var build = new AssetBundleBuild { assetBundleName = BundleName, assetNames = assets };
            BuildPipeline.BuildAssetBundles(OutputFolder, new[] { build }, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            var mapping = new StringBuilder();
            mapping.AppendLine("# Generated by the Lightsaber Animation Kit. form: VanillaClip = BundleClip");
            foreach (SaberAnimMap.Entry e in map.entries.Where(x => x.replacement != null))
                mapping.AppendLine($"{e.form}: {e.VanillaName} = {e.replacement.name}");
            File.WriteAllText(Path.Combine(OutputFolder, MappingName), mapping.ToString());

            string summary = $"[Lightsaber] Built {Path.GetFullPath(Path.Combine(OutputFolder, BundleName))} and {MappingName}.";
            if (!string.IsNullOrEmpty(map.modFolder) && Directory.Exists(map.modFolder))
            {
                File.Copy(Path.Combine(OutputFolder, BundleName), Path.Combine(map.modFolder, BundleName), true);
                File.Copy(Path.Combine(OutputFolder, MappingName), Path.Combine(map.modFolder, MappingName), true);
                summary += $" Copied to {map.modFolder}.";
            }
            else
            {
                summary += " Copy both files next to LightsaberMod.dll.";
            }
            Debug.Log(summary);
            EditorUtility.RevealInFinder(Path.Combine(OutputFolder, BundleName));
        }

        private static SaberAnimMap FindMap()
        {
            SaberAnimMap map = Selection.activeObject as SaberAnimMap;
            if (map == null)
            {
                string guid = AssetDatabase.FindAssets("t:SaberAnimMap").FirstOrDefault();
                if (guid != null) map = AssetDatabase.LoadAssetAtPath<SaberAnimMap>(AssetDatabase.GUIDToAssetPath(guid));
            }
            if (map == null)
                Debug.LogError("[Lightsaber] No SaberAnimMap found. Create one: Assets > Create > Lightsaber > Animation Map.");
            return map;
        }
    }
}
