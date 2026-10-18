using System.Collections.Generic;
using System.Linq;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Held-button Force lightning: arcs from the wielder's hand into the nearest enemy in
    /// front, then chains to others nearby. Damage ticks five times a second and stamina
    /// drains for as long as the key is held.
    /// </summary>
    internal static class ForceLightning
    {
        private const float Range = 10f;
        private const float ConeDegrees = 45f;
        private const float ChainRange = 5f;
        private const int MaxTargets = 3;
        private const float TickSeconds = 0.2f;
        private const float BoltRefreshSeconds = 0.05f;
        private const int BoltPoints = 12;

        private static readonly Color BoltColor = new Color(0.65f, 0.75f, 1f);
        private static readonly List<Character> Scratch = new List<Character>();
        private static readonly List<Character> Targets = new List<Character>();

        private static GameObject _fx;
        private static LineRenderer[] _bolts;
        private static Light _light;
        private static AudioSource _sound;
        private static Player _fxOwner;
        private static float _nextTick, _nextBoltRefresh;
        private static bool _active;
        private static bool _exhausted; // ran dry: wait for the key to be released before trying again

        /// <summary>Call every frame with whether the key is held.</summary>
        public static void Update(Player player, bool held)
        {
            if (_active && (_fx == null || _fxOwner != player))
            {
                // Relogged or respawned mid-channel: the effects went with the old body.
                _active = false;
                Targets.Clear();
            }

            if (!held) _exhausted = false;
            if (!held || _exhausted || !CanChannel(player))
            {
                Stop();
                return;
            }

            if (!_active)
            {
                _active = true;
                _nextTick = 0f;
                EnsureFx(player);
                _fx.SetActive(true);
                _sound.volume = SaberAudio.Volume;
                _sound.Play();
            }

            SaberForm form = FormManager.Current;
            if (Time.time >= _nextTick)
            {
                _nextTick = Time.time + TickSeconds;
                float cost = LightsaberPlugin.LightningStaminaPerSecond.Value * TickSeconds * form.ForceCost;
                if (!player.HaveStamina(cost))
                {
                    player.Message(MessageHud.MessageType.Center, "Not enough stamina");
                    _exhausted = true;
                    Stop();
                    return;
                }
                Compat.Call(player, "UseStamina", cost);

                FindTargets(player);
                Strike(player, form);
            }

            if (Time.time >= _nextBoltRefresh)
            {
                _nextBoltRefresh = Time.time + BoltRefreshSeconds;
                DrawBolts(player);
            }
        }

        public static void Stop()
        {
            if (!_active) return;
            _active = false;
            Targets.Clear();
            if (_fx != null) _fx.SetActive(false);
        }

        private static bool CanChannel(Player player)
        {
            return player != null && !player.IsDead() && SaberFactory.IsSaber(player.GetCurrentWeapon()) && !SaberThrow.InFlight;
        }

        private static void FindTargets(Player player)
        {
            Targets.Clear();
            Vector3 origin = player.GetCenterPoint();
            Vector3 forward = player.transform.forward;

            Scratch.Clear();
            Character.GetCharactersInRange(origin, Range, Scratch);
            Character first = Scratch
                .Where(c => IsValid(player, c))
                .Where(c => Vector3.Angle(forward, Flat(c.GetCenterPoint() - origin)) <= ConeDegrees / 2f)
                .OrderBy(c => Vector3.Distance(origin, c.GetCenterPoint()))
                .FirstOrDefault();
            if (first == null) return;

            // Chain: each further arc jumps to the closest enemy not yet struck.
            Targets.Add(first);
            while (Targets.Count < MaxTargets)
            {
                Character from = Targets[Targets.Count - 1];
                Scratch.Clear();
                Character.GetCharactersInRange(from.GetCenterPoint(), ChainRange, Scratch);
                Character next = Scratch
                    .Where(c => IsValid(player, c) && !Targets.Contains(c))
                    .OrderBy(c => Vector3.Distance(from.GetCenterPoint(), c.GetCenterPoint()))
                    .FirstOrDefault();
                if (next == null) break;
                Targets.Add(next);
            }
        }

        private static bool IsValid(Player player, Character c) =>
            c != null && c != player && !c.IsDead() && BaseAI.IsEnemy(player, c);

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static void Strike(Player player, SaberForm form)
        {
            float damage = LightsaberPlugin.LightningDamage.Value * form.ForcePower * form.LightningPower;
            ForcePowers.Channeling = true;
            try
            {
                for (int i = 0; i < Targets.Count; i++)
                {
                    Character target = Targets[i];
                    var hit = new HitData();
                    // Each jump down the chain loses a quarter of its bite.
                    hit.m_damage.m_lightning = damage * Mathf.Pow(0.75f, i);
                    hit.m_point = target.GetCenterPoint();
                    hit.m_dir = (target.GetCenterPoint() - player.GetCenterPoint()).normalized;
                    hit.m_staggerMultiplier = 0.4f;
                    hit.m_blockable = false;
                    hit.m_dodgeable = false;
                    hit.SetAttacker(player);
                    target.Damage(hit);
                }
            }
            finally
            {
                ForcePowers.Channeling = false;
            }
        }

        // ----- Visuals ------------------------------------------------------------------------

        private static void EnsureFx(Player player)
        {
            if (_fx != null && _fxOwner == player) return;
            if (_fx != null) Object.Destroy(_fx);

            _fxOwner = player;
            _fx = new GameObject("force_lightning_fx");
            _fx.transform.SetParent(player.transform, false);

            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = BoltColor };

            _bolts = new LineRenderer[MaxTargets + 2];
            for (int i = 0; i < _bolts.Length; i++)
            {
                var go = new GameObject("bolt" + i);
                go.transform.SetParent(_fx.transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = BoltPoints;
                line.sharedMaterial = mat;
                line.startColor = Color.white;
                line.endColor = BoltColor;
                line.widthMultiplier = 0.05f;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                _bolts[i] = line;
            }

            _light = new GameObject("lightning_light").AddComponent<Light>();
            _light.transform.SetParent(_fx.transform, false);
            _light.type = LightType.Point;
            _light.color = BoltColor;
            _light.range = 6f;
            _light.shadows = LightShadows.None;

            _sound = _fx.AddComponent<AudioSource>();
            SaberAudio.Configure(_sound);
            _sound.clip = SaberAudio.Lightning;
            _sound.loop = true;

            _fx.SetActive(false);
        }

        private static Vector3 HandPosition(Player player)
        {
            Transform hand = Compat.GetField<Transform>(player.GetComponent<VisEquipment>(), "m_leftHand");
            return hand != null ? hand.position : player.GetCenterPoint() + player.transform.forward * 0.5f;
        }

        private static void DrawBolts(Player player)
        {
            Vector3 hand = HandPosition(player);
            _light.transform.position = hand;
            _light.intensity = Random.Range(1.5f, 3.5f);

            int used = 0;
            Vector3 from = hand;
            foreach (Character target in Targets)
            {
                if (target == null) continue;
                Vector3 to = target.GetCenterPoint();
                Jag(_bolts[used++], from, to);
                from = to;
            }

            // Nothing to strike, or spare arcs: crackle out into the air in front.
            while (used < _bolts.Length && (Targets.Count == 0 || used < Targets.Count + 1))
            {
                Vector3 dir = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(-25f, 25f), 0f) * player.transform.forward;
                Jag(_bolts[used++], hand, hand + dir * Random.Range(3f, 6f));
            }

            for (int i = 0; i < _bolts.Length; i++)
                _bolts[i].enabled = i < used;
        }

        private static void Jag(LineRenderer line, Vector3 from, Vector3 to)
        {
            float length = Vector3.Distance(from, to);
            for (int i = 0; i < BoltPoints; i++)
            {
                float k = (float)i / (BoltPoints - 1);
                Vector3 p = Vector3.Lerp(from, to, k);
                if (i > 0 && i < BoltPoints - 1)
                    p += Random.insideUnitSphere * Mathf.Min(0.35f, length * 0.06f);
                line.SetPosition(i, p);
            }
        }
    }
}
