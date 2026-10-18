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
            if (JarKai.Active) JarKai.AddOffhandCut(hit);
            float mult = form.OffenceMultiplier(FormManager.NearbyEnemies(player), FormManager.IsConfined(player))
                         + FormManager.TakeRiposte(form)
                         + FormManager.TakeMomentum(form);
            hit.m_damage.Modify(mult);
            hit.m_staggerMultiplier *= form.Stagger;
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

            bool blocking = __instance.IsBlocking();

            // Vaapad: Windu holding Sidious' lightning on his blade and driving it back.
            if (form.RedirectLightning && blocking && hit.m_damage.m_lightning > 0f)
            {
                if (attacker != null && !attacker.IsDead())
                    Redirect(__instance, attacker, hit.m_damage.m_lightning, lightning: true);
                hit.m_damage.m_lightning = 0f;
                __instance.Message(MessageHud.MessageType.TopLeft, "Lightning turned back!");
            }

            bool ranged = IsRanged(hit, attacker, __instance);
            if (form.DeflectBolts && blocking && ranged)
            {
                float incoming = hit.GetTotalDamage();
                if (form.RedirectBolts > 0 && attacker != null && !attacker.IsDead())
                    Redirect(__instance, attacker, incoming * form.RedirectBolts);

                hit.m_damage = new HitData.DamageTypes();
                hit.m_pushForce = 0f;
                hit.m_staggerMultiplier = 0f;
                __instance.Message(MessageHud.MessageType.TopLeft, form.RedirectBolts > 0 ? "Barrier of Blades!" : "Bolt deflected");
                return;
            }

            hit.m_damage.Modify(ranged ? form.DamageTaken * form.RangedTaken : form.DamageTaken);
            if (hit.GetTotalDamage() > 0f)
                FormManager.BreakMomentum();
        }

        private static bool IsRanged(HitData hit, Character attacker, Character self)
        {
            if (RangedField != null && RangedField.GetValue(hit) is bool ranged)
                return ranged;
            // Older builds have no flag: anything from further than a long melee reach counts.
            return attacker != null && Vector3.Distance(attacker.transform.position, self.transform.position) > 5f;
        }

        private static void Redirect(Character self, Character shooter, float damage, bool lightning = false)
        {
            var bolt = new HitData();
            if (lightning)
            {
                bolt.m_damage.m_lightning = damage;
            }
            else
            {
                bolt.m_damage.m_fire = damage * 0.5f;
                bolt.m_damage.m_pierce = damage * 0.5f;
            }
            bolt.m_point = shooter.GetCenterPoint();
            bolt.m_dir = (shooter.GetCenterPoint() - self.GetCenterPoint()).normalized;
            bolt.m_pushForce = 10f;
            bolt.SetAttacker(self);
            ForcePowers.Channeling = true; // keep the form's own damage bonus off the redirected bolt
            try { shooter.Damage(bolt); }
            finally { ForcePowers.Channeling = false; }
        }
    }

    /// <summary>Successful blocks feed Soresu's stamina and arm Djem So's and Makashi's counters.</summary>
    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    internal static class BlockPatch
    {
        // Valheim counts a block as a perfect parry when it lands within this long of raising the guard.
        private const float ParryWindow = 0.25f;

        private static void Prefix(Humanoid __instance, out bool __state)
        {
            // Read before vanilla resolves the block. If the field is missing, every block counts
            // as a parry, so Makashi's counter degrades to Djem So's rather than disappearing.
            __state = Compat.GetField(__instance, "m_blockTimer", 0f) < ParryWindow;
        }

        private static void Postfix(Humanoid __instance, bool __result, bool __state)
        {
            if (!__result) return;
            SaberForm form = FormManager.ActiveFor(__instance);
            if (form == null) return;

            SaberAudio.PlayAt(SaberAudio.Clash, __instance.GetCenterPoint(), 0.9f, Random.Range(0.9f, 1.1f));

            if (form.BlockStaminaRefund > 0)
                Compat.Call(__instance, "AddStamina", form.BlockStaminaRefund);
            FormManager.ArmRiposte((Player)__instance, form, __state);
        }
    }

    /// <summary>Jar'Kai: two blades block better than one. Covers every GetBlockPower overload.</summary>
    [HarmonyPatch]
    internal static class JarKaiBlockPowerPatch
    {
        private static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(ItemDrop.ItemData))
                .FindAll(m => m.Name == "GetBlockPower" && m.ReturnType == typeof(float));

        private static bool Prepare() => System.Linq.Enumerable.Any(TargetMethods());

        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            Player player = Player.m_localPlayer;
            if (JarKai.Active && player != null && __instance == player.GetCurrentWeapon())
                __result *= JarKai.BlockPowerBonus;
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
            if (form != null) __result *= form.AttackStamina * (JarKai.Active ? JarKai.AttackStaminaCost : 1f);
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
            float speed = form != null ? form.EffectiveAttackSpeed(FormManager.IsConfined((Player)character)) : 1f;
            bool want = speed != 1f && character.InAttack();

            if (want)
            {
                // Leave vanilla hit-pause (speed near 0) alone so impacts still land with weight.
                if (animator.speed > 0.9f)
                {
                    animator.speed = speed;
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
            {
                MovesetManager.AfterAttack(player, secondaryAttack);
                Animation.Mocap.MocapPlayer.OnLocalAttack(player, secondaryAttack);
            }
        }
    }
}
