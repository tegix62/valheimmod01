using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightsaberMod.Forms;
using LightsaberMod.Items;
using LightsaberMod.Net;
using LightsaberMod.Util;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Animation
{
    /// <summary>
    /// Optional custom animations per form. If an asset bundle named
    /// <c>lightsaber_animations</c> sits next to the DLL, its clips replace vanilla player
    /// clips while a lightsaber is held, following the rules in <c>lightsaber_animations.txt</c>.
    /// Without the bundle this does nothing.
    /// </summary>
    /// <remarks>
    /// Mapping file format, one rule per line:
    /// <code>
    /// # form id (shiicho, makashi, soresu, ataru, shien, niman, juyo) or * for every form
    /// makashi: VanillaClipName = BundleClipName
    /// *: AnotherVanillaClip = SharedSaberClip
    /// </code>
    /// The <c>lightsaber_clips</c> console command lists the vanilla clip names. Replacement
    /// clips must carry the same animation events as the clip they replace, or hits won't land.
    /// </remarks>
    internal static class FormAnimations
    {
        public const string BundleName = "lightsaber_animations";
        public const string MappingName = "lightsaber_animations.txt";

        // form index (or -1 for "*") -> vanilla clip name -> replacement clip
        private static readonly Dictionary<int, Dictionary<string, AnimationClip>> Rules = new Dictionary<int, Dictionary<string, AnimationClip>>();
        private static readonly Dictionary<RuntimeAnimatorController, AnimatorOverrideController[]> Cache = new Dictionary<RuntimeAnimatorController, AnimatorOverrideController[]>();

        public static bool Enabled { get; private set; }

        public static void Load(string pluginDir)
        {
            string bundlePath = Path.Combine(pluginDir, BundleName);
            string mappingPath = Path.Combine(pluginDir, MappingName);
            if (!File.Exists(bundlePath)) return;
            if (!File.Exists(mappingPath))
            {
                Logger.LogWarning($"Found {BundleName} but no {MappingName}; custom animations are off");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Logger.LogWarning($"{BundleName} failed to load (built with a different Unity version?)");
                return;
            }

            Dictionary<string, AnimationClip> clips = bundle.LoadAllAssets<AnimationClip>().ToDictionary(c => c.name, c => c);
            int lineNo = 0;
            foreach (string raw in File.ReadAllLines(mappingPath))
            {
                lineNo++;
                string line = raw.Split('#')[0].Trim();
                if (line.Length == 0) continue;

                if (!TryParse(line, out string formId, out string vanilla, out string replacement))
                {
                    Logger.LogWarning($"{MappingName}:{lineNo}: expected 'form: VanillaClip = BundleClip'");
                    continue;
                }

                int form = formId == "*" ? -1 : Array.FindIndex(SaberForm.All, f => f.Id == formId);
                if (formId != "*" && form < 0)
                {
                    Logger.LogWarning($"{MappingName}:{lineNo}: unknown form '{formId}'");
                    continue;
                }
                if (!clips.TryGetValue(replacement, out AnimationClip clip))
                {
                    Logger.LogWarning($"{MappingName}:{lineNo}: no clip '{replacement}' in the bundle");
                    continue;
                }

                if (!Rules.TryGetValue(form, out var map)) Rules[form] = map = new Dictionary<string, AnimationClip>();
                map[vanilla] = clip;
            }

            Enabled = Rules.Count > 0;
            Logger.LogInfo($"Custom saber animations: {clips.Count} clips, {Rules.Values.Sum(r => r.Count)} replacement rules");
        }

        private static bool TryParse(string line, out string form, out string vanilla, out string replacement)
        {
            form = vanilla = replacement = null;
            int colon = line.IndexOf(':');
            int equals = line.IndexOf('=');
            if (colon < 0 || equals < colon) return false;
            form = line.Substring(0, colon).Trim().ToLowerInvariant();
            vanilla = line.Substring(colon + 1, equals - colon - 1).Trim();
            replacement = line.Substring(equals + 1).Trim();
            return form.Length > 0 && vanilla.Length > 0 && replacement.Length > 0;
        }

        /// <summary>The override controller for a form, built once per original controller. Null if no rule touches that form.</summary>
        public static AnimatorOverrideController For(RuntimeAnimatorController original, int form)
        {
            if (!Cache.TryGetValue(original, out AnimatorOverrideController[] perForm))
            {
                perForm = new AnimatorOverrideController[SaberForm.All.Length];
                for (int i = 0; i < perForm.Length; i++)
                    perForm[i] = Build(original, i);
                Cache[original] = perForm;
            }
            return form >= 0 && form < perForm.Length ? perForm[form] : null;
        }

        private static AnimatorOverrideController Build(RuntimeAnimatorController original, int form)
        {
            Rules.TryGetValue(-1, out var shared);
            Rules.TryGetValue(form, out var specific);
            if (shared == null && specific == null) return null;

            var aoc = new AnimatorOverrideController(original) { name = original.name + "_" + SaberForm.All[form].Id };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
            aoc.GetOverrides(overrides);

            int replaced = 0;
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip vanilla = overrides[i].Key;
                AnimationClip clip = null;
                if (specific != null) specific.TryGetValue(vanilla.name, out clip);
                if (clip == null && shared != null) shared.TryGetValue(vanilla.name, out clip);
                if (clip == null) continue;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(vanilla, clip);
                replaced++;
            }

            if (replaced == 0) return null;
            aoc.ApplyOverrides(overrides);
            return aoc;
        }

        /// <summary>Attaches a <see cref="FormAnimator"/> to every player in range. Call periodically.</summary>
        public static void EnsureAnimators()
        {
            if (!Enabled) return;
            foreach (Player p in Player.GetAllPlayers())
                if (p != null && p.GetComponent<FormAnimator>() == null)
                    p.gameObject.AddComponent<FormAnimator>();
        }
    }

    /// <summary>Swaps one player's animator to their form's override while they hold a lightsaber.</summary>
    internal sealed class FormAnimator : MonoBehaviour
    {
        private Player _player;
        private Animator _animator;
        private RuntimeAnimatorController _original;
        private RuntimeAnimatorController _applied;
        private float _nextCheck;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Update()
        {
            if (Time.time < _nextCheck || _player == null) return;
            _nextCheck = Time.time + 0.25f;

            if (_animator == null)
            {
                _animator = Compat.GetField<Animator>(_player, "m_animator");
                if (_animator == null) return;
                _original = _animator.runtimeAnimatorController;
                _applied = _original;
            }

            RuntimeAnimatorController wanted = _original;
            if (HoldingSaber())
            {
                int form = _player == Player.m_localPlayer ? FormManager.CurrentIndex : NetSync.ReadForm(_player);
                wanted = (RuntimeAnimatorController)FormAnimations.For(_original, form) ?? _original;
            }

            if (wanted == _applied) return;

            // Swapping controllers resets animator state; only do it on an actual change (draw, sheathe, new form).
            _animator.runtimeAnimatorController = wanted;
            _applied = wanted;
        }

        /// <summary>
        /// Works for remote players too: their inventories aren't loaded on this client, but
        /// the model in their hand is, and every saber model carries a SaberBlade.
        /// </summary>
        private bool HoldingSaber()
        {
            GameObject right = Compat.GetField<GameObject>(_player.GetComponent<VisEquipment>(), "m_rightItemInstance");
            return right != null && right.GetComponentInChildren<SaberBlade>(true) != null;
        }
    }
}
