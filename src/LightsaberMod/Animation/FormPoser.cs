using System.Collections.Generic;
using LightsaberMod.Forms;
using LightsaberMod.Items;
using LightsaberMod.Net;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Animation
{
    /// <summary>
    /// Procedural animation layered after Valheim's animator each frame, on every player:
    /// a per-form stance while a lightsaber is drawn, and whole-body flourishes (Ataru's
    /// flip, spinning techniques) that rotate the character model.
    /// </summary>
    internal sealed class FormPoser : MonoBehaviour
    {
        public enum Flourish { Flip = 0, Spin = 1 }

        private const float BlendPerSecond = 4f;
        private const float FlipSeconds = 0.6f;
        private const float SpinSeconds = 0.4f;
        private static readonly Vector3 BodyPivot = new Vector3(0f, 0.9f, 0f); // roughly the hips

        // Bone names in Valheim's rig, used only if the avatar isn't humanoid.
        private static readonly Dictionary<HumanBodyBones, string> FallbackNames = new Dictionary<HumanBodyBones, string>
        {
            [HumanBodyBones.Spine] = "Spine", [HumanBodyBones.Chest] = "Spine1",
            [HumanBodyBones.RightUpperArm] = "RightArm", [HumanBodyBones.RightLowerArm] = "RightForeArm", [HumanBodyBones.RightHand] = "RightHand",
            [HumanBodyBones.LeftUpperArm] = "LeftArm", [HumanBodyBones.LeftLowerArm] = "LeftForeArm", [HumanBodyBones.LeftHand] = "LeftHand",
        };

        private Player _player;
        private Animator _animator;
        private Transform _visual;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new Dictionary<HumanBodyBones, Transform>();
        // What the animator produced and what we turned it into, per bone. If a bone still holds
        // our output next frame the animator skipped it (e.g. physics-rate updates), so we
        // re-apply from the same base instead of stacking offsets.
        private readonly Dictionary<Transform, Quaternion> _lastBase = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<Transform, Quaternion> _lastOut = new Dictionary<Transform, Quaternion>();
        private float _weight;
        private string _poseKey;

        private bool _flourishing;
        private Flourish _flourish;
        private float _flourishStart;
        private Vector3 _visualPos;
        private Quaternion _visualRot;

        public static FormPoser For(Player player)
        {
            FormPoser poser = player.GetComponent<FormPoser>();
            return poser != null ? poser : player.gameObject.AddComponent<FormPoser>();
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        public void Play(Flourish flourish)
        {
            if (!LightsaberPlugin.Flourishes.Value || !Resolve() || _visual == null) return;
            if (_flourishing) EndFlourish();
            _flourishing = true;
            _flourish = flourish;
            _flourishStart = Time.time;
            _visualPos = _visual.localPosition;
            _visualRot = _visual.localRotation;
        }

        private bool Resolve()
        {
            if (_animator != null) return true;
            _animator = Compat.GetField<Animator>(_player, "m_animator");
            if (_animator == null) return false;

            // Flourishes rotate the visual model only. Never the player root, which carries the
            // physics body; if the model can't be told apart from it, flourishes are skipped.
            GameObject visual = Compat.GetField<GameObject>(_player, "m_visual");
            _visual = visual != null ? visual.transform : _animator.transform;
            if (_visual == _player.transform) _visual = null;

            _bones.Clear();
            _lastBase.Clear();
            _lastOut.Clear();
            foreach (HumanBodyBones bone in PoseLibrary.Bones)
            {
                Transform t = _animator.isHuman ? _animator.GetBoneTransform(bone) : null;
                if (t == null && FallbackNames.TryGetValue(bone, out string name))
                    t = FindDeep(_animator.transform, name);
                if (t != null) _bones[bone] = t;
            }
            return true;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private void LateUpdate()
        {
            if (_player == null || !Resolve()) return;

            UpdateStance();
            if (_flourishing) UpdateFlourish();
        }

        // ----- Stance -------------------------------------------------------------------------

        private void UpdateStance()
        {
            bool drawn = SaberBlade.InRightHand(_player);
            string key = drawn ? CurrentPoseKey() : null;

            // Stances are for standing ready: attacks, blocks and flourishes use the game's own motion.
            // A motion-capture idle clip, if one is mapped, replaces the code-driven stance.
            Mocap.MocapPlayer mocap = GetComponent<Mocap.MocapPlayer>();
            bool posing = key != null && !_player.IsDead() && !_player.InAttack() && !_player.IsBlocking() && !_flourishing
                          && (mocap == null || !mocap.IdleActive);
            float target = posing ? LightsaberPlugin.StanceIntensity.Value : 0f;
            _weight = Mathf.MoveTowards(_weight, target, BlendPerSecond * Time.deltaTime);
            if (key != null) _poseKey = key;
            if (_weight <= 0f) return;

            Dictionary<HumanBodyBones, Vector3> pose = PoseLibrary.Get(_poseKey);
            if (pose == null) return;
            foreach (var entry in pose)
            {
                if (!_bones.TryGetValue(entry.Key, out Transform bone)) continue;
                Quaternion current = bone.localRotation;
                Quaternion baseRot = _lastOut.TryGetValue(bone, out Quaternion prevOut) && current == prevOut
                    ? _lastBase[bone]
                    : current;
                bone.localRotation = baseRot * Quaternion.Euler(entry.Value * _weight);
                _lastBase[bone] = baseRot;
                _lastOut[bone] = bone.localRotation;
            }
        }

        private string CurrentPoseKey()
        {
            bool local = _player == Player.m_localPlayer;
            bool jarKai = local ? JarKai.Active : NetSync.ReadJarKai(_player).Length > 0;
            if (jarKai) return "jarkai";
            int form = local ? FormManager.CurrentIndex : NetSync.ReadForm(_player);
            return form >= 0 && form < SaberForm.All.Length ? SaberForm.All[form].Id : null;
        }

        // ----- Flourishes ---------------------------------------------------------------------

        private void UpdateFlourish()
        {
            float duration = _flourish == Flourish.Flip ? FlipSeconds : SpinSeconds;
            float k = (Time.time - _flourishStart) / duration;
            if (k >= 1f || _player.IsDead())
            {
                EndFlourish();
                return;
            }

            // Ease in and out so the body doesn't snap at either end.
            float angle = 360f * Mathf.SmoothStep(0f, 1f, k);
            Quaternion r = _flourish == Flourish.Flip
                ? Quaternion.AngleAxis(angle, Vector3.right)   // forward somersault
                : Quaternion.AngleAxis(angle, Vector3.up);     // full turn

            // Rotate the model about its middle rather than its feet.
            _visual.localRotation = r * _visualRot;
            _visual.localPosition = _visualPos + (BodyPivot - r * BodyPivot);
        }

        private void EndFlourish()
        {
            _flourishing = false;
            if (_visual == null) return;
            _visual.localRotation = _visualRot;
            _visual.localPosition = _visualPos;
        }

        private void OnDisable()
        {
            if (_flourishing) EndFlourish();
        }
    }
}
