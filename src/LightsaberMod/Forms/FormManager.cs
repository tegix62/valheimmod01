using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Tracks the local player's chosen form, keeps its status effect in sync with whether a
    /// lightsaber is in hand, and answers combat questions for the Harmony patches.
    /// </summary>
    internal static class FormManager
    {
        // Vanilla items whose icons stand in for each form on the status bar.
        private static readonly string[] IconSources = { "SwordIron", "KnifeSilver", "ShieldBronzeBuckler", "Feathers", "SurtlingCore", "Crystal", "Bloodbag" };

        private static readonly Dictionary<string, StatusEffect> Effects = new Dictionary<string, StatusEffect>();
        private static readonly List<Character> Scratch = new List<Character>();

        private static int _current;
        private static StatusEffect _applied;
        private static Player _appliedTo;
        private static int _publishedForm = -1;
        private static float _nextSync;
        private static float _riposteUntil;
        private static int _nearbyEnemies;
        private static float _nearbyCountedAt = -10f;
        private static bool _confined;
        private static float _confinedCheckedAt = -10f;
        private static int _momentum;
        private static float _momentumUntil;

        public static SaberForm Current => SaberForm.All[_current];
        public static int CurrentIndex => _current;

        public static void Init(int savedForm)
        {
            _current = Mathf.Clamp(savedForm, 0, SaberForm.All.Length - 1);
        }

        public static void RegisterStatusEffects()
        {
            for (int i = 0; i < SaberForm.All.Length; i++)
            {
                SaberForm form = SaberForm.All[i];
                var se = ScriptableObject.CreateInstance<SE_Stats>();
                se.name = form.StatusEffectName;
                se.m_name = form.Title;
                se.m_tooltip = form.Epithet + " (" + form.AltName + ")\n" + form.Tooltip + "\n\n" + MovesetManager.Describe(i);
                se.m_ttl = 0f;
                se.m_icon = BorrowIcon(IconSources[i]) ?? BorrowIcon("Crystal");

                // Fields set by name: their presence varies between game versions.
                if (form.MoveSpeed != 0) Compat.SetField(se, "m_speedModifier", form.MoveSpeed);
                if (form.JumpBoost != 0) Compat.SetField(se, "m_jumpModifier", new Vector3(0f, form.JumpBoost, 0f));
                if (form.StaminaRegen != 1f) Compat.SetField(se, "m_staminaRegenMultiplier", form.StaminaRegen);

                ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(se, false));
                Effects[form.Id] = se;
            }
        }

        private static Sprite BorrowIcon(string prefabName)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
            Sprite[] icons = prefab?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons;
            return icons != null && icons.Length > 0 ? icons[0] : null;
        }

        // ----- Selection --------------------------------------------------------------------

        public static void Cycle(Player player, int step)
        {
            int n = SaberForm.All.Length;
            for (int tries = 1; tries <= n; tries++)
            {
                int candidate = ((_current + step * tries) % n + n) % n;
                if (IsUnlocked(player, SaberForm.All[candidate], out _))
                {
                    Select(player, candidate);
                    return;
                }
            }
        }

        public static void Select(Player player, int index)
        {
            SaberForm form = SaberForm.All[index];
            if (!IsUnlocked(player, form, out float needed))
            {
                player.Message(MessageHud.MessageType.Center, $"{form.Title} requires skill {needed:0} with this weapon");
                return;
            }

            _current = index;
            LightsaberPlugin.SavedForm.Value = index;
            _nextSync = 0f;
            FormMoveset moves = FormMoveset.All[index];
            player.Message(MessageHud.MessageType.Center,
                $"{form.Title}\n<size=18>{form.Epithet} — {form.Summary}\n{moves.Primary.Name} / {moves.Secondary.Name}</size>");
        }

        /// <summary>Forms unlock as the wielder's skill with the held saber's weapon type rises.</summary>
        public static bool IsUnlocked(Player player, SaberForm form, out float needed)
        {
            needed = (form.Number - 1) * LightsaberPlugin.UnlockSkillStep.Value;
            if (needed <= 0) return true;

            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (!SaberFactory.IsSaber(weapon)) return true; // checked again when a saber is drawn
            return player.GetSkills().GetSkillLevel(weapon.m_shared.m_skillType) >= needed;
        }

        // ----- Per-frame upkeep -----------------------------------------------------------------

        public static void Tick(Player player)
        {
            if (player != _appliedTo)
            {
                // New character, respawn or relog: the old status effect went with the old body.
                Reset();
                _appliedTo = player;
                _nextSync = 0f;
                _publishedForm = -1;
            }

            if (_publishedForm != _current)
            {
                Net.NetSync.PublishForm(player, _current);
                _publishedForm = _current;
            }

            if (Time.time < _nextSync) return;
            _nextSync = Time.time + 0.25f;

            bool armed = SaberFactory.IsSaber(player.GetCurrentWeapon());
            if (armed && !IsUnlocked(player, Current, out _))
                _current = 0;
            JarKai.Tick(player); // also clears Jar'Kai when the saber is sheathed
            if (armed)
                MovesetManager.Apply(player);

            StatusEffect wanted = armed && Effects.TryGetValue(Current.Id, out StatusEffect se) ? se : null;
            if (wanted == _applied) return;

            SEMan seman = player.GetSEMan();
            if (_applied != null)
                Compat.Call(seman, "RemoveStatusEffect", _applied.NameHash());
            if (wanted != null)
                Compat.Call(seman, "AddStatusEffect", wanted.NameHash(), true);
            _applied = wanted;
        }

        public static void Reset()
        {
            _applied = null;
            _riposteUntil = 0f;
        }

        // ----- Queries for patches ------------------------------------------------------------

        /// <summary>The active form when <paramref name="c"/> is the local player holding a lightsaber, else null.</summary>
        public static SaberForm ActiveFor(Character c)
        {
            Player local = Player.m_localPlayer;
            if (local == null || c != local) return null;
            return SaberFactory.IsSaber(local.GetCurrentWeapon()) ? Current : null;
        }

        public static int NearbyEnemies(Player player)
        {
            if (Time.time - _nearbyCountedAt < 0.5f) return _nearbyEnemies;
            _nearbyCountedAt = Time.time;

            Scratch.Clear();
            Character.GetCharactersInRange(player.transform.position, 8f, Scratch);
            int count = 0;
            foreach (Character c in Scratch)
                if (c != player && !c.IsDead() && BaseAI.IsEnemy(player, c))
                    count++;
            _nearbyEnemies = count;
            return count;
        }

        /// <summary>
        /// Ataru needs room to move: true indoors, in dungeons and under a roof. Falls back to
        /// "open ground" if the game's cover checks can't be found.
        /// </summary>
        public static bool IsConfined(Player player)
        {
            if (Time.time - _confinedCheckedAt < 1f) return _confined;
            _confinedCheckedAt = Time.time;
            _confined = Compat.Call(player, "InInterior") is bool inside && inside
                || Compat.CallStatic("Cover", "IsUnderRoof", player.transform.position) is bool roofed && roofed;
            return _confined;
        }

        /// <param name="perfectParry">Whether the block landed inside the parry window.</param>
        public static void ArmRiposte(Player player, SaberForm form, bool perfectParry)
        {
            if (form.RiposteBonus <= 0 || (form.RiposteOnParryOnly && !perfectParry)) return;
            _riposteUntil = Time.time + 2.5f;
            player.Message(MessageHud.MessageType.TopLeft, form.RiposteName);
        }

        /// <summary>Consumes a pending riposte, returning its bonus (0 if none).</summary>
        public static float TakeRiposte(SaberForm form)
        {
            if (form.RiposteBonus <= 0 || Time.time > _riposteUntil) return 0f;
            _riposteUntil = 0f;
            return form.RiposteBonus;
        }

        /// <summary>Vaapad: the bonus from the current run of hits, which this hit then extends.</summary>
        public static float TakeMomentum(SaberForm form)
        {
            if (form.MomentumPerHit <= 0) return 0f;
            if (Time.time > _momentumUntil) _momentum = 0;
            float bonus = Mathf.Min(form.MomentumMax, _momentum * form.MomentumPerHit);
            _momentum++;
            _momentumUntil = Time.time + 4f;
            return bonus;
        }

        /// <summary>Vaapad: taking a hit breaks the flow.</summary>
        public static void BreakMomentum() => _momentum = 0;
    }
}
