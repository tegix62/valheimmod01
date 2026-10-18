using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// One of the seven classical lightsaber combat forms. All multipliers are 1 = vanilla.
    /// A form only takes effect while a lightsaber is in hand.
    /// </summary>
    /// <remarks>
    /// Each form's strengths, weaknesses and technique names follow the lore (mostly the
    /// Legends material that Disney canon has not contradicted). The numbers are gameplay
    /// translations of those traits; docs/FORMS.md explains each choice and its source.
    /// </remarks>
    internal sealed class SaberForm
    {
        public int Number;
        public string Id;
        public string Name;
        public string Epithet;            // "Way of the ..."
        public string AltName;            // "... Form"
        public string Summary;            // one line shown on switch
        public string Tooltip;            // status effect tooltip

        // Offence
        public float DamageDealt = 1f;
        public float AttackStamina = 1f;
        public float AttackSpeed = 1f;
        public float Stagger = 1f;
        public float CrowdBonusPerEnemy;  // Shii-Cho: extra damage per additional nearby enemy
        public float CrowdBonusCap;
        public float LonePenalty;         // Shii-Cho: damage lost against a single opponent
        public float DuelBonus;           // Makashi: extra damage when only one enemy is near
        public float CrowdPenalty;        // Makashi: damage lost when surrounded
        public bool ConfinedPenalty;      // Ataru: offence bonuses vanish indoors and under roofs
        public float MomentumPerHit;      // Vaapad: damage gained per consecutive hit
        public float MomentumMax;

        // Defence
        public float DamageTaken = 1f;
        public float RangedTaken = 1f;    // Makashi: poor blaster-deflection training
        public bool DeflectBolts;         // blocking negates arrows, spit and spells
        public float RedirectBolts;       // fraction of a deflected bolt's damage sent back at the shooter
        public bool RedirectLightning;    // Vaapad: blocking turns lightning back on its source
        public float BlockStaminaRefund;  // flat stamina returned per successful block
        public float RiposteBonus;        // bonus to the next hit after a block
        public bool RiposteOnParryOnly;   // Makashi: only a perfectly timed parry opens the counter
        public string RiposteName;

        // Movement and the Force
        public float MoveSpeed;           // additive, e.g. 0.15 = +15%
        public float JumpBoost;           // additive jump force
        public float StaminaRegen = 1f;
        public float ForcePower = 1f;
        public float ForceCost = 1f;
        public float ThrowPower = 1f;     // saber throw damage
        public bool DarkSide;             // may call Force lightning when it's restricted to the dark side
        public float LeapPower = 1f;      // Force leap height/distance and landing strike
        public float LeapCost = 1f;

        public string StatusEffectName => "SE_LightsaberForm_" + Id;

        public static readonly SaberForm[] All =
        {
            // Lore: the oldest, most basic form, taught to every Jedi. Wide sweeping strikes built
            // for many opponents at once; its contact focus is sun djem, disarming. Weak against
            // a single skilled, mobile opponent, and no specialised blaster defence.
            new SaberForm
            {
                Number = 1, Id = "shiicho", Name = "Shii-Cho", Epithet = "Way of the Sarlacc", AltName = "Determination Form",
                Summary = "The first form: broad sweeps for many foes, aimed at disarming.",
                Tooltip = "+8% damage for each extra enemy nearby (max +40%)\n-10% damage against a lone enemy\n+20% stagger (sun djem: disarming)\n-10% attack stamina",
                CrowdBonusPerEnemy = 0.08f, CrowdBonusCap = 0.40f, LonePenalty = 0.10f,
                Stagger = 1.2f, AttackStamina = 0.9f,
            },

            // Lore: created for lightsaber-versus-lightsaber duels. Precise, economical, fencing-like.
            // Strong against one opponent, weak against several, and poorly trained for blaster
            // deflection. Named techniques: Makashi Riposte (turn the blade aside, counter at once)
            // and Contentious Opportunity (seize an opening).
            new SaberForm
            {
                Number = 2, Id = "makashi", Name = "Makashi", Epithet = "Way of the Ysalamiri", AltName = "Contention Form",
                Summary = "The duellist's form: precise and economical, made for blade-on-blade.",
                Tooltip = "+35% damage against a lone enemy\n-15% damage when 3+ enemies are near\nContentious Opportunity: a perfect parry makes your next hit +40%\n-20% attack stamina\n+25% damage from ranged attacks",
                DuelBonus = 0.35f, CrowdPenalty = 0.15f, AttackStamina = 0.8f,
                RiposteBonus = 0.4f, RiposteOnParryOnly = true, RiposteName = "Contentious Opportunity",
                RangedTaken = 1.25f,
            },

            // Lore: developed as blasters spread. Tight, minimal movements that leave no opening and
            // deflect fire; the practitioner outlasts the opponent. Almost purely defensive.
            // Named technique: Circle of Shelter (a defensive perimeter around the user and allies).
            new SaberForm
            {
                Number = 3, Id = "soresu", Name = "Soresu", Epithet = "Way of the Mynock", AltName = "Resilience Form",
                Summary = "Tight, unbreakable defence. Outlast the enemy; deflect every bolt.",
                Tooltip = "Blocking deflects all ranged attacks\n-25% damage taken\n+8 stamina per block\n+25% stamina regen\n-20% damage dealt",
                DeflectBolts = true, DamageTaken = 0.75f, BlockStaminaRefund = 8f,
                StaminaRegen = 1.25f, DamageDealt = 0.8f, MoveSpeed = -0.05f,
            },

            // Lore: acrobatic, aggressive, Force-assisted leaps, flips and spins. Weak in confined
            // spaces (it needs room to move) and in prolonged fights, since it burns stamina fast.
            // Named technique: Hawk-Bat Swoop, a strike so fast it lands before the foe can react.
            new SaberForm
            {
                Number = 4, Id = "ataru", Name = "Ataru", Epithet = "Way of the Hawk-Bat", AltName = "Aggression Form",
                Summary = "Acrobatic aggression. Needs open ground, and burns stamina quickly.",
                Tooltip = "+20% attack speed, +15% damage\n+15% move speed, higher jumps\nForce Leap 40% stronger, half cost\nNo attack speed or damage bonus indoors or under a roof\n+25% attack stamina\n+10% damage taken",
                AttackSpeed = 1.2f, DamageDealt = 1.15f, MoveSpeed = 0.15f, JumpBoost = 0.5f,
                LeapPower = 1.4f, LeapCost = 0.5f, ConfinedPenalty = true,
                AttackStamina = 1.25f, DamageTaken = 1.1f,
            },

            // Lore: two variants built on Soresu's defence. Shien redirects blaster bolts back at the
            // shooters (Barrier of Blades). Djem So blocks and counter-attacks at once with power
            // (Fluid Riposte, Falling Avalanche). The saber throw bonus reflects its best-known
            // practitioners, Anakin Skywalker/Darth Vader, rather than any stated doctrine.
            new SaberForm
            {
                Number = 5, Id = "shien", Name = "Shien / Djem So", Epithet = "Way of the Krayt Dragon", AltName = "Perseverance Form",
                Summary = "Absorb, then strike back. Bolts return to the shooter; blocks become counters.",
                Tooltip = "Barrier of Blades: blocking sends 75% of a ranged attack back at the shooter\nFluid Riposte: next hit after a block deals +50%\n+15% damage, +50% stagger\nSaber throw +50% damage\n-10% damage taken",
                DeflectBolts = true, RedirectBolts = 0.75f, RiposteBonus = 0.5f, RiposteName = "Fluid Riposte",
                ThrowPower = 1.5f, DamageDealt = 1.15f, Stagger = 1.5f, DamageTaken = 0.9f,
            },

            // Lore: a balanced blend of the earlier forms that integrates Force powers into
            // combat; the foundation for Jar'Kai. Its weakness is breadth without depth: no
            // standout strength.
            new SaberForm
            {
                Number = 6, Id = "niman", Name = "Niman", Epithet = "Way of the Rancor", AltName = "Moderation Form",
                Summary = "Moderation in all things, with the Force woven into every strike.",
                Tooltip = "Force powers 75% stronger and 40% cheaper\n+5% damage, -5% damage taken\n+10% stamina regen\nNo single standout strength",
                ForcePower = 1.75f, ForceCost = 0.6f, DamageDealt = 1.05f, DamageTaken = 0.95f,
                StaminaRegen = 1.1f, AttackStamina = 0.95f,
            },

            // Lore: the most demanding form; seemingly erratic, ferocious, drawing on intense
            // emotion, and so close to the dark side that the Council restricted it (Sith such as
            // Maul used it freely). Mace Windu's Vaapad channels the darkness of the duel as a
            // loop between the fighters and turns an opponent's power back on them. In Revenge
            // of the Sith Windu holds Sidious' lightning on his blade and drives it back.
            new SaberForm
            {
                Number = 7, Id = "juyo", Name = "Juyo / Vaapad", Epithet = "Way of the Vornskr", AltName = "Ferocity Form",
                Summary = "Ferocious and unpredictable, riding the darkness of the duel.",
                Tooltip = "+25% damage, +15% attack speed, +25% stagger\nEach consecutive hit adds 5% damage (up to +30%); being hit breaks the flow\nBlocking turns lightning back on its source\nThe dark side: the only form that can call Force lightning\n+25% damage taken\n-15% stamina regen",
                DamageDealt = 1.25f, AttackSpeed = 1.15f, Stagger = 1.25f,
                MomentumPerHit = 0.05f, MomentumMax = 0.30f, RedirectLightning = true, DarkSide = true,
                DamageTaken = 1.25f, StaminaRegen = 0.85f,
            },
        };

        public string RomanNumeral => new[] { "I", "II", "III", "IV", "V", "VI", "VII" }[Number - 1];
        public string Title => $"Form {RomanNumeral}: {Name}";

        public float OffenceMultiplier(int nearbyEnemies, bool confined)
        {
            float mult = DamageDealt;
            if (ConfinedPenalty && confined && mult > 1f)
                mult = 1f;
            if (CrowdBonusPerEnemy > 0 && nearbyEnemies > 1)
                mult += Mathf.Min(CrowdBonusCap, CrowdBonusPerEnemy * (nearbyEnemies - 1));
            if (LonePenalty > 0 && nearbyEnemies <= 1)
                mult -= LonePenalty;
            if (DuelBonus > 0 && nearbyEnemies <= 1)
                mult += DuelBonus;
            if (CrowdPenalty > 0 && nearbyEnemies >= 3)
                mult -= CrowdPenalty;
            return mult;
        }

        public float EffectiveAttackSpeed(bool confined) =>
            ConfinedPenalty && confined ? Mathf.Min(1f, AttackSpeed) : AttackSpeed;
    }
}
