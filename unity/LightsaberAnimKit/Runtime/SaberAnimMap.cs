using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightsaberAnimKit
{
    /// <summary>
    /// Which clip replaces which vanilla Valheim clip, per lightsaber form. Create one with
    /// Assets > Create > Lightsaber > Animation Map, fill it in, then use the Lightsaber menu.
    /// </summary>
    [CreateAssetMenu(menuName = "Lightsaber/Animation Map", fileName = "SaberAnimMap")]
    public class SaberAnimMap : ScriptableObject
    {
        public static readonly string[] Forms = { "*", "shiicho", "makashi", "soresu", "ataru", "shien", "niman", "juyo" };

        [Serializable]
        public class Entry
        {
            [Tooltip("shiicho, makashi, soresu, ataru, shien, niman, juyo, or * for every form")]
            public string form = "makashi";

            [Tooltip("The vanilla clip exported from Valheim (AssetRipper). Its animation events (hit timing, chain windows) are copied onto the replacement.")]
            public AnimationClip vanillaClip;

            [Tooltip("Only needed if vanillaClip isn't set: the vanilla clip's name, as printed by the lightsaber_clips console command.")]
            public string vanillaClipName;

            [Tooltip("Your clip (Mixamo, mocap, hand-made). Import its model as Humanoid.")]
            public AnimationClip replacement;

            public string VanillaName => vanillaClip != null ? vanillaClip.name : vanillaClipName;
        }

        public List<Entry> entries = new List<Entry>();

        [Tooltip("Optional: the mod's plugin folder (where LightsaberMod.dll lives). The build is copied there.")]
        public string modFolder = "";
    }
}
