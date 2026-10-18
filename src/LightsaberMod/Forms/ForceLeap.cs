using System.Collections.Generic;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Force Leap: press jump again in mid-air with a lightsaber drawn to launch forward and
    /// up. Landing from a leap slams the blade down, hitting everything around you, and
    /// the Force cushions the fall. Ataru leaps further, hits harder and costs half as much.
    /// </summary>
    internal static class ForceLeap
    {
        private const float ForwardSpeed = 9f;
        private const float UpSpeed = 8f;
        private const float SlamRadius = 3.5f;

        private static readonly List<Character> Scratch = new List<Character>();

        private static bool _usedThisAirtime;
        private static bool _leaping;
        private static float _leapStartedAt;
        private static Player _player;

        /// <param name="canInput">False while a menu or chat has focus: landing still resolves, no new leap starts.</param>
        public static void Update(Player player, bool canInput)
        {
            if (player != _player)
            {
                _player = player;
                _leaping = false;
                _usedThisAirtime = false;
            }

            bool onGround = player.IsOnGround();
            if (_leaping)
            {
                // Keep resetting the fall-height record so the leap never causes fall damage.
                Compat.SetField(player, "m_maxAirAltitude", player.transform.position.y);

                // Short grace period: the frame of the leap can still report ground contact.
                if (onGround && Time.time - _leapStartedAt > 0.2f)
                {
                    _leaping = false;
                    Slam(player);
                }
            }

            if (onGround)
            {
                _usedThisAirtime = false;
                return;
            }

            if (!canInput || _usedThisAirtime || !JumpPressed() || player.IsSwimming() || !SaberFactory.IsSaber(player.GetCurrentWeapon()))
                return;

            SaberForm form = FormManager.Current;
            float cost = LightsaberPlugin.LeapStaminaCost.Value * form.LeapCost;
            if (!player.HaveStamina(cost))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to leap");
                _usedThisAirtime = true;
                return;
            }

            Rigidbody body = Compat.GetField<Rigidbody>(player, "m_body");
            if (body == null) return;

            Compat.Call(player, "UseStamina", cost);
            Vector3 forward = player.transform.forward;
            body.velocity = new Vector3(forward.x, 0f, forward.z) * ForwardSpeed * form.LeapPower + Vector3.up * UpSpeed * form.LeapPower;

            _usedThisAirtime = true;
            _leaping = true;
            _leapStartedAt = Time.time;
            SaberAudio.PlayAt(SaberAudio.Ignite, player.GetCenterPoint(), 0.5f, 1.6f);
            player.Message(MessageHud.MessageType.TopLeft, "Force Leap");
        }

        private static bool JumpPressed()
        {
            return Compat.CallStatic("ZInput", "GetButtonDown", "Jump") is bool kb && kb
                || Compat.CallStatic("ZInput", "GetButtonDown", "JoyJump") is bool pad && pad;
        }

        private static void Slam(Player player)
        {
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (!SaberFactory.IsSaber(weapon)) return;

            SaberForm form = FormManager.Current;
            ItemDrop.ItemData.SharedData shared = weapon.m_shared;
            int levels = Mathf.Max(0, weapon.m_quality - 1);
            float scale = 0.6f * form.LeapPower;

            Vector3 center = player.transform.position;
            Scratch.Clear();
            Character.GetCharactersInRange(center, SlamRadius, Scratch);

            int hits = 0;
            ForcePowers.Channeling = true;
            try
            {
                foreach (Character target in Scratch)
                {
                    if (target == player || target.IsDead() || !BaseAI.IsEnemy(player, target)) continue;
                    var hit = new HitData();
                    hit.m_damage.m_slash = (shared.m_damages.m_slash + shared.m_damagesPerLevel.m_slash * levels) * scale;
                    hit.m_damage.m_fire = (shared.m_damages.m_fire + shared.m_damagesPerLevel.m_fire * levels) * scale;
                    hit.m_point = target.GetCenterPoint();
                    Vector3 away = target.transform.position - center;
                    away.y = 0f;
                    hit.m_dir = away.sqrMagnitude > 0.01f ? away.normalized : player.transform.forward;
                    hit.m_pushForce = 60f * form.LeapPower;
                    hit.m_staggerMultiplier = 2.5f;
                    hit.SetAttacker(player);
                    target.Damage(hit);
                    hits++;
                }
            }
            finally
            {
                ForcePowers.Channeling = false;
            }

            SaberAudio.PlayAt(SaberAudio.Clash, center, 1f, 0.7f);
            if (hits > 0)
                player.Message(MessageHud.MessageType.TopLeft, $"Leaping Strike! ({hits})");
        }
    }
}
