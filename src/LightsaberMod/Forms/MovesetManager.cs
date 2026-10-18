using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Jotunn.Managers;
using LightsaberMod.Items;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Builds each form's attacks from vanilla templates and swaps them onto the held
    /// lightsaber when the form changes.
    /// </summary>
    /// <remarks>
    /// Attacks live on the item's SharedData, which every copy of that saber prefab shares.
    /// Only the owner's client runs a player's attacks, so retuning it locally never
    /// changes how other players' sabers swing.
    /// </remarks>
    internal static class MovesetManager
    {
        private static readonly MethodInfo CloneMethod = AccessTools.Method(typeof(object), "MemberwiseClone");

        private static readonly Dictionary<SaberMove, Attack> Built = new Dictionary<SaberMove, Attack>();
        private static readonly Dictionary<ItemDrop.ItemData.SharedData, int> AppliedForm = new Dictionary<ItemDrop.ItemData.SharedData, int>();

        private static float _lastPrimaryAt = -10f;
        private static int _lastPoolPick = -1;

        public static bool Enabled => LightsaberPlugin.UseFormMovesets.Value;

        /// <summary>Resolves every move against the vanilla weapons. Call once vanilla prefabs exist.</summary>
        public static void Build()
        {
            foreach (FormMoveset set in FormMoveset.All)
            {
                BuildMove(set.Primary);
                BuildMove(set.Secondary);
                if (set.PrimaryPool != null)
                    foreach (SaberMove move in set.PrimaryPool)
                        BuildMove(move);
            }
        }

        private static void BuildMove(SaberMove move)
        {
            if (move == null || Built.ContainsKey(move)) return;

            foreach (string donor in move.Donors)
            {
                ItemDrop.ItemData.SharedData shared = PrefabManager.Instance.GetPrefab(donor)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
                Attack source = move.DonorSecondary ? shared?.m_secondaryAttack : shared?.m_attack;
                if (source == null || string.IsNullOrEmpty(source.m_attackAnimation)) continue;

                Attack attack = Clone(source);
                attack.m_damageMultiplier *= move.Damage;
                attack.m_attackRange *= move.Range;
                attack.m_attackAngle *= move.Arc;
                attack.m_attackStamina *= move.Stamina;
                attack.m_forceMultiplier *= move.Force;
                attack.m_staggerMultiplier *= move.Stagger;
                Built[move] = attack;
                Logger.LogDebug($"{move.Name}: using {(move.DonorSecondary ? "secondary" : "primary")} attack of {donor} ({attack.m_attackAnimation})");
                return;
            }

            Logger.LogWarning($"No vanilla weapon found for {move.Name}; the saber keeps its own attack for it");
        }

        private static Attack Clone(Attack attack) => (Attack)CloneMethod.Invoke(attack, null);

        /// <summary>Puts the current form's moves on the held saber if they aren't there already.</summary>
        public static void Apply(Player player)
        {
            if (!Enabled) return;
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (!SaberFactory.IsSaber(weapon)) return;

            ItemDrop.ItemData.SharedData shared = weapon.m_shared;
            int form = FormManager.CurrentIndex;
            if (AppliedForm.TryGetValue(shared, out int applied) && applied == form) return;

            FormMoveset set = FormMoveset.All[form];
            if (Built.TryGetValue(set.Primary, out Attack primary)) shared.m_attack = Clone(primary);
            if (Built.TryGetValue(set.Secondary, out Attack secondary)) shared.m_secondaryAttack = Clone(secondary);
            AppliedForm[shared] = form;
            _lastPoolPick = -1;
        }

        /// <summary>
        /// Juyo: when a new combo starts (rather than continuing one), draw a different move
        /// from the pool so no two flurries look alike.
        /// </summary>
        public static void BeforeAttack(Player player, bool secondary)
        {
            if (!Enabled || secondary) return;
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (!SaberFactory.IsSaber(weapon)) return;

            FormMoveset set = FormMoveset.All[FormManager.CurrentIndex];
            bool newCombo = Time.time - _lastPrimaryAt > 1.2f;
            _lastPrimaryAt = Time.time;
            if (set.PrimaryPool == null || set.PrimaryPool.Length < 2 || !newCombo) return;

            int pick;
            do pick = Random.Range(0, set.PrimaryPool.Length);
            while (pick == _lastPoolPick);
            _lastPoolPick = pick;

            if (Built.TryGetValue(set.PrimaryPool[pick], out Attack attack))
                weapon.m_shared.m_attack = Clone(attack);
        }

        /// <summary>Announces secondary techniques by name and fires Niman's Force-guided push.</summary>
        public static void AfterAttack(Player player, bool secondary)
        {
            if (!Enabled || !secondary || !SaberFactory.IsSaber(player.GetCurrentWeapon())) return;

            SaberMove move = FormMoveset.All[FormManager.CurrentIndex].Secondary;
            player.Message(MessageHud.MessageType.TopLeft, move.Name);
            if (move.ForcePushOnUse)
                ForcePowers.Push(player, quiet: true);
        }

        public static string Describe(int formIndex)
        {
            FormMoveset set = FormMoveset.All[formIndex];
            return $"Primary: {set.Primary.Name}. {set.Primary.Lore}\nSecondary: {set.Secondary.Name}. {set.Secondary.Lore}";
        }
    }
}
