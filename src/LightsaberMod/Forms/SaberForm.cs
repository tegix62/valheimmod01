using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// One of the seven classical lightsaber combat forms. All multipliers are 1 = vanilla.
    /// A form only takes effect while a lightsaber is in hand.
    /// </summary>
    internal sealed class SaberForm
    {
        public int Number;
        public string Id;
        public string Name;
        public string Epithet;
        public string Summary;            // one line shown on switch
        public string Tooltip;            // status effect tooltip

        // Offence
        public float DamageDealt = 1f;
        public float AttackStamina = 1f;
        public float AttackSpeed = 1f;
        public float Stagger = 1f;
        public float CrowdBonusPerEnemy;  // Shii-Cho: extra damage per additional nearby enemy
        public float CrowdBonusCap;
        public float DuelBonus;           // Makashi: extra damage when only one enemy is near
        public float CrowdPenalty;        // Makashi: damage lost when surrounded
        public float Lifesteal;           // Juyo/Vaapad: fraction of damage dealt returned as health

        // Defence
        public float DamageTaken = 1f;
        public bool DeflectBolts;         // blocking negates arrows, spit and spells
        public float RedirectBolts;       // fraction of a deflected bolt's damage sent back at the shooter
        public float BlockStaminaRefund;  // flat stamina returned per successful block
        public float RiposteBonus;        // bonus to the next hit after a block (Djem So)

        // Movement and the Force
        public float MoveSpeed;           // additive, e.g. 0.15 = +15%
        public float JumpBoost;           // additive jump force
        public float StaminaRegen = 1f;
        public float ForcePower = 1f;
        public float ForceCost = 1f;

        public string StatusEffectName => "SE_LightsaberForm_" + Id;

        public static readonly SaberForm[] All =
        {
            new SaberForm
            {
                Number = 1, Id = "shiicho", Name = "Shii-Cho", Epithet = "Way of the Sarlacc",
                Summary = "Wide sweeps that grow stronger the more foes surround you.",
                Tooltip = "+8% damage for each extra enemy nearby (max +40%)\n-10% attack stamina\n-5% damage taken",
                CrowdBonusPerEnemy = 0.08f, CrowdBonusCap = 0.40f,
                AttackStamina = 0.9f, DamageTaken = 0.95f,
            },
            new SaberForm
            {
                Number = 2, Id = "makashi", Name = "Makashi", Epithet = "Way of the Ysalamiri",
                Summary = "Precise duelling. Deadly against one foe, exposed against many.",
                Tooltip = "+35% damage against a lone enemy\n-15% damage when 3+ enemies are near\n+30% stagger\n-20% attack stamina",
                DuelBonus = 0.35f, CrowdPenalty = 0.15f, Stagger = 1.3f, AttackStamina = 0.8f,
            },
            new SaberForm
            {
                Number = 3, Id = "soresu", Name = "Soresu", Epithet = "Way of the Mynock",
                Summary = "Tight, unbreakable defence. Blocking deflects every bolt.",
                Tooltip = "Blocking deflects all ranged attacks\n-25% damage taken\n+8 stamina per block\n+25% stamina regen\n-20% damage dealt",
                DeflectBolts = true, DamageTaken = 0.75f, BlockStaminaRefund = 8f,
                StaminaRegen = 1.25f, DamageDealt = 0.8f, MoveSpeed = -0.05f,
            },
            new SaberForm
            {
                Number = 4, Id = "ataru", Name = "Ataru", Epithet = "Way of the Hawk-Bat",
                Summary = "Acrobatic aggression. Faster blades, higher leaps, heavy stamina use.",
                Tooltip = "+20% attack speed\n+15% damage\n+15% move speed\nHigher jumps\n+25% attack stamina\n+10% damage taken",
                AttackSpeed = 1.2f, DamageDealt = 1.15f, MoveSpeed = 0.15f, JumpBoost = 0.5f,
                AttackStamina = 1.25f, DamageTaken = 1.1f,
            },
            new SaberForm
            {
                Number = 5, Id = "shien", Name = "Shien / Djem So", Epithet = "Way of the Krayt Dragon",
                Summary = "Absorb, then strike back. Blocks fuel your counter and hurl bolts home.",
                Tooltip = "Blocking deflects ranged attacks back at the shooter (75%)\nNext hit after a block: +50% damage\n+15% damage, +50% stagger\n-10% damage taken",
                DeflectBolts = true, RedirectBolts = 0.75f, RiposteBonus = 0.5f,
                DamageDealt = 1.15f, Stagger = 1.5f, DamageTaken = 0.9f,
            },
            new SaberForm
            {
                Number = 6, Id = "niman", Name = "Niman", Epithet = "Way of the Rancor",
                Summary = "Balanced bladework woven with the Force.",
                Tooltip = "Force powers 75% stronger and 40% cheaper\n+5% damage, -5% damage taken\n+15% stamina regen",
                ForcePower = 1.75f, ForceCost = 0.6f, DamageDealt = 1.05f, DamageTaken = 0.95f,
                StaminaRegen = 1.15f, AttackStamina = 0.95f,
            },
            new SaberForm
            {
                Number = 7, Id = "juyo", Name = "Juyo / Vaapad", Epithet = "Way of the Vornskr",
                Summary = "Ferocious, unpredictable, and fed by the fury of battle.",
                Tooltip = "+40% damage, +15% attack speed\n8% of damage dealt heals you\n+25% stagger\n+25% damage taken\n-15% stamina regen",
                DamageDealt = 1.4f, AttackSpeed = 1.15f, Lifesteal = 0.08f, Stagger = 1.25f,
                DamageTaken = 1.25f, StaminaRegen = 0.85f,
            },
        };

        public string RomanNumeral => new[] { "I", "II", "III", "IV", "V", "VI", "VII" }[Number - 1];
        public string Title => $"Form {RomanNumeral}: {Name}";

        public float OffenceMultiplier(int nearbyEnemies)
        {
            float mult = DamageDealt;
            if (CrowdBonusPerEnemy > 0 && nearbyEnemies > 1)
                mult += Mathf.Min(CrowdBonusCap, CrowdBonusPerEnemy * (nearbyEnemies - 1));
            if (DuelBonus > 0 && nearbyEnemies <= 1)
                mult += DuelBonus;
            if (CrowdPenalty > 0 && nearbyEnemies >= 3)
                mult -= CrowdPenalty;
            return mult;
        }
    }
}
