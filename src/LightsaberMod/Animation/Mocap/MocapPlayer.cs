using LightsaberMod.Forms;
using LightsaberMod.Items;
using LightsaberMod.Net;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Animation.Mocap
{
    /// <summary>
    /// Plays motion-capture clips on one player (local or remote) by retargeting them through
    /// Unity's humanoid muscle space, after Valheim's animator has run each frame. The vanilla
    /// attack keeps playing underneath, so hit timing and damage are unchanged; only how the
    /// body moves is replaced. By default only the spine, arms and head are driven.
    /// </summary>
    internal sealed class MocapPlayer : MonoBehaviour
    {
        public enum Slot { Primary = 0, Secondary = 1 }

        private const float BlendIn = 0.08f;
        private const float BlendOut = 0.15f;
        private const float ComboWindow = 1.2f;

        private static bool[] _upperBodyMuscles;

        private Player _player;
        private Animator _animator;
        private HumanPoseHandler _target;
        private HumanPose _pose, _mocap;
        private bool _unsupported;

        private MocapEntry _attack;
        private float _attackTime;
        private MocapEntry _idle;
        private float _idleTime;
        private float _weight;
        private MocapEntry _blendingFrom;  // what's on screen while weight is fading
        private float _blendingTime;

        // Local player only: which step of a primary chain this is.
        private int _chainStep;
        private float _lastPrimary = -10f;

        public static MocapPlayer For(Player player)
        {
            MocapPlayer p = player.GetComponent<MocapPlayer>();
            return p != null ? p : player.gameObject.AddComponent<MocapPlayer>();
        }

        /// <summary>True when an idle clip is driving this player, so the code-driven stance should stand down.</summary>
        public bool IdleActive => _idle != null && _attack == null && _weight > 0f;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        /// <summary>Called on the attacker's own client when an attack starts; plays and broadcasts it.</summary>
        public static void OnLocalAttack(Player player, bool secondary)
        {
            if (!MocapLibrary.Enabled || !SaberBlade.InRightHand(player)) return;
            MocapPlayer p = For(player);

            int step = 0;
            if (!secondary)
            {
                p._chainStep = Time.time - p._lastPrimary < ComboWindow ? p._chainStep + 1 : 0;
                p._lastPrimary = Time.time;
                step = p._chainStep;
            }

            Slot slot = secondary ? Slot.Secondary : Slot.Primary;
            if (p.Play(slot, step))
                NetSync.BroadcastMocap(player, (int)slot, step);
        }

        public bool Play(Slot slot, int step)
        {
            MocapEntry entry = MocapLibrary.Get(PoseKey(), slot == Slot.Secondary ? "secondary" : "primary", step);
            if (entry == null || !Resolve()) return false;
            _attack = entry;
            _attackTime = entry.Start;
            return true;
        }

        private bool Resolve()
        {
            if (_target != null) return true;
            if (_unsupported) return false;

            _animator = Compat.GetField<Animator>(_player, "m_animator");
            if (_animator == null) return false;
            if (!_animator.isHuman || _animator.avatar == null)
            {
                _unsupported = true;
                Jotunn.Logger.LogWarning("Player animator isn't humanoid; motion capture can't be retargeted onto it");
                return false;
            }

            _target = new HumanPoseHandler(_animator.avatar, _animator.transform);
            _pose.muscles = new float[HumanTrait.MuscleCount];
            _mocap.muscles = new float[HumanTrait.MuscleCount];

            if (_upperBodyMuscles == null)
            {
                _upperBodyMuscles = new bool[HumanTrait.MuscleCount];
                for (int i = 0; i < HumanTrait.MuscleCount; i++)
                {
                    string m = HumanTrait.MuscleName[i];
                    _upperBodyMuscles[i] = !(m.Contains("Leg") || m.Contains("Foot") || m.Contains("Toes"));
                }
            }
            return true;
        }

        private string PoseKey()
        {
            bool local = _player == Player.m_localPlayer;
            if (local ? JarKai.Active : NetSync.ReadJarKai(_player).Length > 0) return "jarkai";
            int form = local ? FormManager.CurrentIndex : NetSync.ReadForm(_player);
            return form >= 0 && form < SaberForm.All.Length ? SaberForm.All[form].Id : "*";
        }

        private void LateUpdate()
        {
            if (!MocapLibrary.Enabled || _player == null) return;
            float dt = Time.deltaTime;

            // Attack clips run to their end; the vanilla attack underneath decides the hit.
            if (_attack != null)
            {
                _attackTime += dt * _attack.Speed;
                if (_attackTime >= _attack.End || _player.IsDead())
                {
                    _blendingFrom = _attack;
                    _blendingTime = _attack.End;
                    _attack = null;
                }
            }

            // Idle loops while standing ready with a saber drawn.
            bool ready = SaberBlade.InRightHand(_player) && !_player.IsDead() && !_player.InAttack() && !_player.IsBlocking();
            MocapEntry idle = ready && Resolve() ? MocapLibrary.Get(PoseKey(), "idle", 0) : null;
            if (idle != _idle) { _idle = idle; _idleTime = idle?.Start ?? 0f; }
            if (_idle != null)
            {
                _idleTime += dt * _idle.Speed;
                if (_idleTime >= _idle.End) _idleTime = _idle.Start + (_idleTime - _idle.End) % Mathf.Max(0.05f, _idle.End - _idle.Start);
            }

            MocapEntry showing = _attack ?? _idle;
            float target = showing != null ? 1f : 0f;
            _weight = Mathf.MoveTowards(_weight, target, dt / (target > _weight ? BlendIn : BlendOut));
            if (showing != null) { _blendingFrom = showing; _blendingTime = _attack != null ? _attackTime : _idleTime; }
            if (_weight <= 0f || _blendingFrom == null || !Resolve()) return;

            Apply(_blendingFrom, _blendingTime, _weight);
        }

        private void Apply(MocapEntry entry, float time, float weight)
        {
            _target.GetHumanPose(ref _pose);       // what Valheim's animator produced this frame
            entry.Rig.Sample(time, ref _mocap);

            for (int i = 0; i < _pose.muscles.Length; i++)
                if (entry.FullBody || _upperBodyMuscles[i])
                    _pose.muscles[i] = Mathf.Lerp(_pose.muscles[i], _mocap.muscles[i], weight);

            // Body position and facing stay Valheim's, so the character never leaves its collider.
            _target.SetHumanPose(ref _pose);
        }

        private void OnDestroy()
        {
            _target?.Dispose();
        }
    }
}
