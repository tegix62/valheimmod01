using System.Collections.Generic;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>Force Push and Force Pull: a cone of telekinetic force in front of the wielder.</summary>
    internal static class ForcePowers
    {
        /// <summary>True while the mod itself is dealing damage, so form bonuses aren't applied twice.</summary>
        public static bool Channeling;

        private const float Range = 9f;
        private const float ConeDegrees = 70f;

        private static readonly List<Character> Scratch = new List<Character>();
        private static float _readyAt;

        public static void Push(Player player) => Use(player, pull: false);

        public static void Pull(Player player) => Use(player, pull: true);

        private static void Use(Player player, bool pull)
        {
            if (!SaberFactory.IsSaber(player.GetCurrentWeapon()))
            {
                player.Message(MessageHud.MessageType.Center, "Draw a lightsaber to focus the Force");
                return;
            }

            if (Time.time < _readyAt)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"The Force needs {_readyAt - Time.time:0.0}s to settle");
                return;
            }

            SaberForm form = FormManager.Current;
            float cost = LightsaberPlugin.ForceStaminaCost.Value * form.ForceCost;
            if (!player.HaveStamina(cost))
            {
                player.Message(MessageHud.MessageType.Center, "Not enough stamina");
                return;
            }

            Vector3 origin = player.GetCenterPoint();
            Vector3 forward = player.transform.forward;
            Scratch.Clear();
            Character.GetCharactersInRange(origin, Range, Scratch);

            int affected = 0;
            Channeling = true;
            try
            {
                foreach (Character target in Scratch)
                {
                    if (target == player || target.IsDead() || !BaseAI.IsEnemy(player, target)) continue;

                    Vector3 toTarget = target.GetCenterPoint() - origin;
                    toTarget.y = 0f;
                    if (Vector3.Angle(forward, toTarget) > ConeDegrees / 2f) continue;

                    float falloff = Mathf.Lerp(1f, 0.5f, toTarget.magnitude / Range);
                    var hit = new HitData();
                    hit.m_damage.m_blunt = (pull ? 4f : 10f) * form.ForcePower * falloff;
                    hit.m_pushForce = (pull ? 70f : 140f) * form.ForcePower * falloff;
                    hit.m_staggerMultiplier = pull ? 2f : 4f;
                    hit.m_point = target.GetCenterPoint();
                    // Pushback follows m_dir, so a pull simply points it back at the wielder.
                    hit.m_dir = pull ? -toTarget.normalized : toTarget.normalized;
                    hit.m_blockable = false;
                    hit.m_dodgeable = false;
                    hit.SetAttacker(player);
                    target.Damage(hit);
                    affected++;
                }
            }
            finally
            {
                Channeling = false;
            }

            Compat.Call(player, "UseStamina", cost);
            _readyAt = Time.time + LightsaberPlugin.ForceCooldown.Value;

            string power = pull ? "Force Pull" : "Force Push";
            player.Message(MessageHud.MessageType.TopLeft, affected > 0 ? $"{power}! ({affected})" : power);
        }
    }
}
