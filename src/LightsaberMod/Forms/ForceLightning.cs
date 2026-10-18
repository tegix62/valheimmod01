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
    /// <remarks>
    /// The local client owns damage and stamina; the arcs, light and sound live in a
    /// <see cref="LightningFx"/> on the caster, which other clients drive themselves after
    /// being told the channel started (see <see cref="Net.NetSync"/>).
    /// </remarks>
    internal static class ForceLightning
    {
        public const float Range = 10f;
        public const int MaxTargets = 3;

        private const float ConeDegrees = 45f;
        private const float ChainRange = 5f;
        private const float TickSeconds = 0.2f;

        private static readonly List<Character> Scratch = new List<Character>();

        private static LightningFx _fx;
        private static float _nextTick;
        private static bool _exhausted; // ran dry: wait for the key to be released before trying again
        private static bool _warned;    // explained the dark-side restriction for this key press

        /// <summary>Call every frame with whether the key is held.</summary>
        public static void Update(Player player, bool held)
        {
            if (_fx != null && _fx.Caster != player)
                _fx = null; // relogged or respawned: the effects went with the old body

            if (!held)
            {
                _exhausted = false;
                _warned = false;
            }

            if (held && LightsaberPlugin.LightningDarkSideOnly.Value && !FormManager.Current.DarkSide && !LightsaberPlugin.FullPowers.Value)
            {
                if (!_warned)
                    player.Message(MessageHud.MessageType.Center, "Force lightning answers only to the dark side (Form VII: Juyo)");
                _warned = true;
                Stop();
                return;
            }

            if (!held || _exhausted || !CanChannel(player))
            {
                Stop();
                return;
            }

            if (_fx == null || !_fx.Channeling)
            {
                _fx = LightningFx.For(player);
                _fx.Begin(computeTargets: false);
                _nextTick = 0f;
                Net.NetSync.BroadcastLightning(player, true);
            }

            if (Time.time < _nextTick) return;
            _nextTick = Time.time + TickSeconds;

            SaberForm form = FormManager.Current;
            float cost = LightsaberPlugin.Cost(LightsaberPlugin.LightningStaminaPerSecond.Value * TickSeconds * form.ForceCost);
            if (!player.HaveStamina(cost))
            {
                player.Message(MessageHud.MessageType.Center, "Not enough stamina");
                _exhausted = true;
                Stop();
                return;
            }
            Compat.Call(player, "UseStamina", cost);

            FindTargets(player, _fx.Targets);
            Strike(player, form, _fx.Targets);
        }

        public static void Stop()
        {
            if (_fx == null || !_fx.Channeling) return;
            _fx.End();
            Net.NetSync.BroadcastLightning(_fx.Caster, false);
        }

        private static bool CanChannel(Player player)
        {
            return player != null && !player.IsDead() && SaberFactory.IsSaber(player.GetCurrentWeapon()) && !SaberThrow.InFlight;
        }

        /// <summary>Nearest enemy in front, then a chain of nearest neighbours. Also used by remote clients for the visuals.</summary>
        public static void FindTargets(Player caster, List<Character> targets)
        {
            targets.Clear();
            Vector3 origin = caster.GetCenterPoint();
            Vector3 forward = caster.transform.forward;

            Scratch.Clear();
            Character.GetCharactersInRange(origin, Range, Scratch);
            Character first = Scratch
                .Where(c => IsValid(caster, c))
                .Where(c => Vector3.Angle(forward, Flat(c.GetCenterPoint() - origin)) <= ConeDegrees / 2f)
                .OrderBy(c => Vector3.Distance(origin, c.GetCenterPoint()))
                .FirstOrDefault();
            if (first == null) return;

            targets.Add(first);
            while (targets.Count < MaxTargets)
            {
                Character from = targets[targets.Count - 1];
                Scratch.Clear();
                Character.GetCharactersInRange(from.GetCenterPoint(), ChainRange, Scratch);
                Character next = Scratch
                    .Where(c => IsValid(caster, c) && !targets.Contains(c))
                    .OrderBy(c => Vector3.Distance(from.GetCenterPoint(), c.GetCenterPoint()))
                    .FirstOrDefault();
                if (next == null) break;
                targets.Add(next);
            }
        }

        private static bool IsValid(Player caster, Character c) =>
            c != null && c != caster && !c.IsDead() && BaseAI.IsEnemy(caster, c);

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static void Strike(Player player, SaberForm form, List<Character> targets)
        {
            // Scales with the saber in hand, so the lightning keeps pace with your gear.
            float damage = PowerScaling.CurrentTotal(player.GetCurrentWeapon()) * LightsaberPlugin.LightningDamage.Value * form.ForcePower;
            ForcePowers.Channeling = true;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Character target = targets[i];
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
    }

    /// <summary>Arcs, flickering light and crackle for one caster. Lives on the caster's GameObject.</summary>
    internal sealed class LightningFx : MonoBehaviour
    {
        private const float BoltRefreshSeconds = 0.05f;
        private const float RemoteRetargetSeconds = 0.2f;
        private const int BoltPoints = 12;

        private static readonly Color BoltColor = new Color(0.65f, 0.75f, 1f);
        private static Material _material;

        public readonly List<Character> Targets = new List<Character>();
        public Player Caster { get; private set; }
        public bool Channeling { get; private set; }

        private LineRenderer[] _bolts;
        private Light _light;
        private AudioSource _sound;
        private bool _computeTargets;
        private float _nextBoltRefresh, _nextRetarget;

        public static LightningFx For(Player caster)
        {
            LightningFx fx = caster.GetComponent<LightningFx>();
            if (fx == null)
            {
                fx = caster.gameObject.AddComponent<LightningFx>();
                fx.Caster = caster;
            }
            return fx;
        }

        /// <param name="computeTargets">True on other clients, which pick their own targets for the arcs.</param>
        public void Begin(bool computeTargets)
        {
            EnsureParts();
            _computeTargets = computeTargets;
            Channeling = true;
            _nextRetarget = 0f;
            _sound.volume = SaberAudio.Volume;
            _sound.Play();
            _light.enabled = true;
        }

        public void End()
        {
            Channeling = false;
            Targets.Clear();
            if (_bolts == null) return;
            foreach (LineRenderer bolt in _bolts) bolt.enabled = false;
            _light.enabled = false;
            _sound.Stop();
        }

        private void LateUpdate()
        {
            if (!Channeling) return;
            if (Caster == null || Caster.IsDead())
            {
                End();
                return;
            }

            if (_computeTargets && Time.time >= _nextRetarget)
            {
                _nextRetarget = Time.time + RemoteRetargetSeconds;
                ForceLightning.FindTargets(Caster, Targets);
            }

            if (Time.time >= _nextBoltRefresh)
            {
                _nextBoltRefresh = Time.time + BoltRefreshSeconds;
                DrawBolts();
            }
        }

        private void EnsureParts()
        {
            if (_bolts != null) return;

            if (_material == null)
            {
                Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                _material = new Material(shader) { color = BoltColor };
            }

            var root = new GameObject("force_lightning_fx");
            root.transform.SetParent(transform, false);

            _bolts = new LineRenderer[ForceLightning.MaxTargets + 2];
            for (int i = 0; i < _bolts.Length; i++)
            {
                var go = new GameObject("bolt" + i);
                go.transform.SetParent(root.transform, false);
                LineRenderer line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = BoltPoints;
                line.sharedMaterial = _material;
                line.startColor = Color.white;
                line.endColor = BoltColor;
                line.widthMultiplier = 0.05f;
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.4f));
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                _bolts[i] = line;
            }

            _light = new GameObject("lightning_light").AddComponent<Light>();
            _light.transform.SetParent(root.transform, false);
            _light.type = LightType.Point;
            _light.color = BoltColor;
            _light.range = 6f;
            _light.shadows = LightShadows.None;
            _light.enabled = false;

            _sound = root.AddComponent<AudioSource>();
            SaberAudio.Configure(_sound);
            _sound.clip = SaberAudio.Lightning;
            _sound.loop = true;
        }

        private Vector3 HandPosition()
        {
            Transform hand = Compat.GetField<Transform>(Caster.GetComponent<VisEquipment>(), "m_leftHand");
            return hand != null ? hand.position : Caster.GetCenterPoint() + Caster.transform.forward * 0.5f;
        }

        private void DrawBolts()
        {
            Vector3 hand = HandPosition();
            _light.transform.position = hand;
            _light.intensity = Random.Range(1.5f, 3.5f);

            int used = 0;
            Vector3 from = hand;
            foreach (Character target in Targets)
            {
                if (target == null || used >= _bolts.Length) continue;
                Vector3 to = target.GetCenterPoint();
                Jag(_bolts[used++], from, to);
                from = to;
            }

            // Nothing to strike, or one spare arc: crackle out into the air in front.
            int stray = Targets.Count == 0 ? 2 : 1;
            for (int i = 0; i < stray && used < _bolts.Length; i++)
            {
                Vector3 dir = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(-25f, 25f), 0f) * Caster.transform.forward;
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
