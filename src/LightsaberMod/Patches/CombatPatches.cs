using System.Reflection;
using HarmonyLib;
using LightsaberMod.Forms;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Patches
{
    /// <summary>Outgoing damage: runs on the attacker's client before the hit is sent to the target's owner.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class OutgoingDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || ForcePowers.Channeling) return;

            Character attacker = hit.GetAttacker();
            SaberForm form = FormManager.ActiveFor(attacker);
            if (form == null || __instance == attacker) return;

            var player = (Player)attacker;
            float mult = form.OffenceMultiplier(FormManager.NearbyEnemies(player)) + FormManager.TakeRiposte(form);
            hit.m_damage.Modify(mult);
            hit.m_staggerMultiplier *= form.Stagger;

            if (form.Lifesteal > 0 && !__instance.IsDead())
            {
                // Estimated before armour; capped so Vaapad can't out-heal a boss.
                float heal = Mathf.Min(hit.GetTotalDamage() * form.Lifesteal, LightsaberPlugin.LifestealCap.Value);
                if (heal > 0.5f) Compat.Call(player, "Heal", heal, true);
            }
        }
    }

    /// <summary>Incoming damage: runs on the local player's own client, before blocking is resolved.</summary>
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class IncomingDamagePatch
    {
        private static readonly FieldInfo RangedField = AccessTools.Field(typeof(HitData), "m_ranged");

        private static void Prefix(Character __instance, HitData hit)
        {
            SaberForm form = FormManager.ActiveFor(__instance);
            if (form == null || hit == null) return;

            Character attacker = hit.GetAttacker();
            if (attacker == __instance) return;

            if (form.DeflectBolts && __instance.IsBlocking() && IsRanged(hit, attacker, __instance))
            {
                float incoming = hit.GetTotalDamage();
                if (form.RedirectBolts > 0 && attacker != null && !attacker.IsDead())
                    Redirect(__instance, attacker, incoming * form.RedirectBolts);

                hit.m_damage = new HitData.DamageTypes();
                hit.m_pushForce = 0f;
                hit.m_staggerMultiplier = 0f;
                __instance.Message(MessageHud.MessageType.TopLeft, form.RedirectBolts > 0 ? "Bolt redirected!" : "Bolt deflected");
                return;
            }

            hit.m_damage.Modify(form.DamageTaken);
        }

        private static bool IsRanged(HitData hit, Character attacker, Character self)
        {
            if (RangedField != null && RangedField.GetValue(hit) is bool ranged)
                return ranged;
            // Older builds have no flag: anything from further than a long melee reach counts.
            return attacker != null && Vector3.Distance(attacker.transform.position, self.transform.position) > 5f;
        }

        private static void Redirect(Character self, Character shooter, float damage)
        {
            var bolt = new HitData();
            bolt.m_damage.m_fire = damage * 0.5f;
            bolt.m_damage.m_pierce = damage * 0.5f;
            bolt.m_point = shooter.GetCenterPoint();
            bolt.m_dir = (shooter.GetCenterPoint() - self.GetCenterPoint()).normalized;
            bolt.m_pushForce = 10f;
            bolt.SetAttacker(self);
            ForcePowers.Channeling = true; // keep the form's own damage bonus off the redirected bolt
            try { shooter.Damage(bolt); }
            finally { ForcePowers.Channeling = false; }
        }
    }

    /// <summary>Successful blocks feed Soresu's stamina and arm Djem So's counter-attack.</summary>
    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    internal static class BlockPatch
    {
        private static void Postfix(Humanoid __instance, bool __result)
        {
            if (!__result) return;
            SaberForm form = FormManager.ActiveFor(__instance);
            if (form == null) return;

            SaberAudio.PlayAt(SaberAudio.Clash, __instance.GetCenterPoint(), 0.9f, Random.Range(0.9f, 1.1f));

            if (form.BlockStaminaRefund > 0)
                Compat.Call(__instance, "AddStamina", form.BlockStaminaRefund);
            FormManager.ArmRiposte(form);
        }
    }

    /// <summary>Scales the stamina each swing costs.</summary>
    [HarmonyPatch]
    internal static class AttackStaminaPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(Attack), "GetAttackStamina");

        private static bool Prepare() => TargetMethod() != null;

        private static void Postfix(Attack __instance, ref float __result)
        {
            SaberForm form = FormManager.ActiveFor(Compat.GetField<Humanoid>(__instance, "m_character"));
            if (form != null) __result *= form.AttackStamina;
        }
    }

    /// <summary>
    /// Speeds up attack animations for Ataru and Juyo. The animator speed is only touched while
    /// the player is attacking and is put back to 1 as soon as the attack ends.
    /// </summary>
    [HarmonyPatch]
    internal static class AttackSpeedPatch
    {
        private static bool _boosted;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(CharacterAnimEvent), "CustomFixedUpdate") ??
            AccessTools.Method(typeof(CharacterAnimEvent), "FixedUpdate");

        private static bool Prepare() => TargetMethod() != null;

        private static void Postfix(CharacterAnimEvent __instance)
        {
            var character = Compat.GetField<Character>(__instance, "m_character");
            if (character == null || character != Player.m_localPlayer) return;
            var animator = Compat.GetField<Animator>(__instance, "m_animator");
            if (animator == null) return;

            SaberForm form = FormManager.ActiveFor(character);
            bool want = form != null && form.AttackSpeed != 1f && character.InAttack();

            if (want)
            {
                // Leave vanilla hit-pause (speed near 0) alone so impacts still land with weight.
                if (animator.speed > 0.9f)
                {
                    animator.speed = form.AttackSpeed;
                    _boosted = true;
                }
            }
            else if (_boosted)
            {
                animator.speed = 1f;
                _boosted = false;
            }
        }
    }

    /// <summary>Hooks each swing for Juyo's shifting combos, named techniques and Niman's Force strike.</summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class StartAttackPatch
    {
        private static bool Prefix(Humanoid __instance, bool secondaryAttack)
        {
            if (!(__instance is Player player) || player != Player.m_localPlayer) return true;
            if (SaberThrow.InFlight) return false; // nothing in hand to swing
            MovesetManager.BeforeAttack(player, secondaryAttack);
            return true;
        }

        private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result)
        {
            if (__result && __instance is Player player && player == Player.m_localPlayer)
                MovesetManager.AfterAttack(player, secondaryAttack);
        }
    }
}
