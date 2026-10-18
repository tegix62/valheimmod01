using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace LightsaberMod.Items
{
    /// <summary>
    /// Lightsaber sounds synthesised at runtime, so the mod ships no audio files: a beating
    /// low hum, an ignition sweep, a retract sweep and a crackling clash.
    /// </summary>
    internal static class SaberAudio
    {
        private const int Rate = 44100;

        private static AudioClip _hum, _ignite, _retract, _clash, _lightning;
        private static AudioMixerGroup _sfxGroup;
        private static bool _searchedGroup;

        public static float Volume => LightsaberPlugin.SoundVolume.Value;

        public static AudioClip Hum => _hum ? _hum : _hum = MakeHum();
        public static AudioClip Ignite => _ignite ? _ignite : _ignite = MakeSweep("saber_ignite", 0.45f, 55f, 190f, rising: true);
        public static AudioClip Retract => _retract ? _retract : _retract = MakeSweep("saber_retract", 0.35f, 190f, 50f, rising: false);
        public static AudioClip Clash => _clash ? _clash : _clash = MakeClash();
        public static AudioClip Lightning => _lightning ? _lightning : _lightning = MakeLightning();

        /// <summary>
        /// Valheim's SFX mixer group, borrowed from any loaded vanilla sound so the game's
        /// volume slider applies to sabers too. Null (plain output) if none is found.
        /// </summary>
        public static AudioMixerGroup SfxGroup
        {
            get
            {
                if (!_searchedGroup)
                {
                    _searchedGroup = true;
                    AudioMixerGroup[] groups = Resources.FindObjectsOfTypeAll<AudioSource>()
                        .Select(a => a.outputAudioMixerGroup)
                        .Where(g => g != null)
                        .ToArray();
                    _sfxGroup = groups.FirstOrDefault(g => g.name.ToLowerInvariant().Contains("sfx")) ?? groups.FirstOrDefault();
                }
                return _sfxGroup;
            }
        }

        public static void Configure(AudioSource source)
        {
            source.outputAudioMixerGroup = SfxGroup;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 22f;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
        }

        /// <summary>Plays a one-shot that survives its emitter being destroyed (e.g. on sheathing).</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float pitch = 1f)
        {
            if (clip == null || Volume <= 0f) return;
            var go = new GameObject("saber_sfx");
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            Configure(source);
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * Volume);
            Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        // ----- Synthesis ------------------------------------------------------------------

        private static AudioClip MakeHum()
        {
            // Two seconds, with every partial completing whole cycles so the loop is seamless.
            int n = Rate * 2;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float s = 0.50f * Mathf.Sin(2f * Mathf.PI * 90f * t)
                        + 0.28f * Mathf.Sin(2f * Mathf.PI * 92.5f * t)   // beats against 90 Hz: the classic wobble
                        + 0.22f * Mathf.Sin(2f * Mathf.PI * 180f * t)
                        + 0.08f * Mathf.Sin(2f * Mathf.PI * 270f * t + 0.6f);
                data[i] = s * 0.45f;
            }
            return Clip("saber_hum", data);
        }

        private static AudioClip MakeSweep(string name, float seconds, float fromHz, float toHz, bool rising)
        {
            int n = (int)(Rate * seconds);
            var data = new float[n];
            var rng = new System.Random(rising ? 11 : 17);
            float phase = 0f, noise = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float hz = Mathf.Lerp(fromHz, toHz, Mathf.Sqrt(k));
                phase += 2f * Mathf.PI * hz / Rate;
                noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.25f); // low-passed hiss

                float tone = Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2f);
                float burst = rising ? Mathf.Exp(-k * 9f) : Mathf.Exp(-(1f - k) * 9f);
                float env = rising ? Mathf.Min(1f, k * 12f) : Mathf.Min(1f, (1f - k) * 6f);
                data[i] = (tone * 0.45f + noise * 0.9f * burst) * env * 0.6f;
            }
            return Clip(name, data);
        }

        private static AudioClip MakeClash()
        {
            int n = (int)(Rate * 0.3f);
            var data = new float[n];
            var rng = new System.Random(23);
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float t = (float)i / Rate;
                float crackle = rng.NextDouble() < 0.08 ? (float)rng.NextDouble() * 2f - 1f : 0f;
                float hiss = (float)rng.NextDouble() * 2f - 1f;
                float zap = Mathf.Sin(2f * Mathf.PI * (900f - 600f * k) * t);
                data[i] = (hiss * 0.35f + crackle * 0.9f + zap * 0.3f) * Mathf.Exp(-k * 7f) * 0.7f;
            }
            return Clip("saber_clash", data);
        }

        private static AudioClip MakeLightning()
        {
            // One second of buzzing electrical crackle, cross-faded at the seam so it loops.
            int n = Rate;
            var data = new float[n];
            var rng = new System.Random(31);
            float snap = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                if (rng.NextDouble() < 0.004) snap = 1f;
                snap *= 0.995f;
                float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 120f * t)) * 0.18f;
                float hiss = ((float)rng.NextDouble() * 2f - 1f) * (0.25f + snap * 0.75f);
                data[i] = (buzz + hiss) * 0.55f;
            }
            int fade = Rate / 20;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                data[i] = data[i] * k + data[n - fade + i] * (1f - k);
            }
            return Clip("force_lightning", data);
        }

        private static AudioClip Clip(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
