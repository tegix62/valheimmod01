namespace LightsaberMod.Forms
{
    /// <summary>
    /// One technique: a vanilla attack borrowed for its animation and hit shape, then retuned.
    /// Valheim can only play animations its player rig already has, so each move names the
    /// vanilla weapons whose swing best matches the technique, in order of preference.
    /// </summary>
    internal sealed class SaberMove
    {
        public string Name;
        public string Lore;
        public string[] Donors;          // vanilla prefab names, first one found wins
        public bool DonorSecondary;      // borrow the donor's secondary attack instead of its primary
        public float Damage = 1f;        // multiplies the donor's own damage multiplier
        public float Range = 1f;
        public float Arc = 1f;
        public float Stamina = 1f;
        public float Force = 1f;
        public float Stagger = 1f;
        public bool ForcePushOnUse;      // Niman: the strike carries a Force Push with it
    }

    /// <summary>A form's primary combo and secondary technique.</summary>
    internal sealed class FormMoveset
    {
        public SaberMove Primary;
        public SaberMove[] PrimaryPool;  // Juyo: each new combo draws a random move from here
        public SaberMove Secondary;

        private static readonly string[] Sword = { "SwordIron", "SwordSilver", "SwordBronze" };
        private static readonly string[] Knife = { "KnifeSilver", "KnifeBlackMetal", "KnifeCopper" };
        private static readonly string[] DualKnives = { "KnifeSkollAndHati", "KnifeSilver" };
        private static readonly string[] Spear = { "SpearBronze", "SpearElderbark", "SpearFlint", "SpearWolfFang" };
        private static readonly string[] Atgeir = { "AtgeirIron", "AtgeirBronze", "AtgeirBlackmetal" };
        private static readonly string[] Greatsword = { "THSwordKrom", "BattleaxeCrystal", "Battleaxe", "AtgeirIron" };
        private static readonly string[] Battleaxe = { "Battleaxe", "BattleaxeCrystal", "THSwordKrom", "AtgeirIron" };
        private static readonly string[] Mace = { "MaceIron", "MaceBronze", "MaceSilver" };
        private static readonly string[] Sledge = { "SledgeIron", "SledgeStagbreaker", "SledgeDemolisher" };
        private static readonly string[] Axe = { "AxeIron", "AxeBronze", "AxeBlackMetal" };

        /// <summary>Indexed like <see cref="SaberForm.All"/>.</summary>
        public static readonly FormMoveset[] All =
        {
            // I. Shii-Cho: the first form, broad disarming sweeps meant for many opponents at once.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Sarlacc Sweep", Donors = Battleaxe,
                    Lore = "Broad horizontal arcs that catch everything within reach.",
                    Arc = 1.25f, Damage = 0.9f, Stamina = 0.9f,
                },
                Secondary = new SaberMove
                {
                    Name = "Disarming Circle", Donors = Atgeir, DonorSecondary = true,
                    Lore = "A full spin that knocks the weapons from the hands of every foe around you.",
                    Damage = 1.0f, Stagger = 1.4f,
                },
            },

            // II. Makashi: the duellist's form, fencing thrusts and economy of motion.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Fencer's Thrust", Donors = Spear,
                    Lore = "Precise lunging thrusts with long reach and a narrow line.",
                    Range = 1.15f, Arc = 0.6f, Damage = 1.15f, Stamina = 0.8f,
                },
                Secondary = new SaberMove
                {
                    Name = "Contra-Riposte", Donors = Sword, DonorSecondary = true,
                    Lore = "A single committed counter-cut aimed at the gap in a guard.",
                    Damage = 1.3f, Stagger = 1.3f,
                },
            },

            // III. Soresu: tight, minimal movements that never leave the defender open.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Tight Guard", Donors = Sword,
                    Lore = "Short, economical cuts that never leave your centre line.",
                    Range = 0.9f, Arc = 0.85f, Damage = 0.9f, Stamina = 0.7f,
                },
                Secondary = new SaberMove
                {
                    Name = "Circle of Shelter", Donors = Mace, DonorSecondary = true,
                    Lore = "A hilt-heavy bash that drives attackers back to make space.",
                    Damage = 0.6f, Force = 2.2f, Stagger = 1.8f, Stamina = 0.8f,
                },
            },

            // IV. Ataru: acrobatics, flips and constant aerial motion.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Hawk-Bat Flurry", Donors = DualKnives,
                    Lore = "A blurring chain of quick cuts delivered on the move.",
                    Damage = 0.85f, Stamina = 1.1f,
                },
                Secondary = new SaberMove
                {
                    Name = "Ataru Leap", Donors = DualKnives, DonorSecondary = true,
                    Lore = "A springing aerial strike that closes distance in an instant.",
                    Damage = 1.35f, Stamina = 1.2f,
                },
            },

            // V. Shien / Djem So: powerful overhand blows that turn defence into offence.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Krayt Strikes", Donors = Mace,
                    Lore = "Heavy overhand blows that batter through any guard.",
                    Damage = 1.15f, Stagger = 1.35f, Force = 1.3f,
                },
                Secondary = new SaberMove
                {
                    Name = "Falling Avalanche", Donors = Sledge,
                    Lore = "A two-handed downward blow that shakes the ground around the target.",
                    Damage = 1.4f, Stagger = 1.5f, Stamina = 1.2f,
                },
            },

            // VI. Niman: the diplomat's form, moderate in everything and paired with the Force.
            new FormMoveset
            {
                Primary = new SaberMove
                {
                    Name = "Rancor Balance", Donors = Sword,
                    Lore = "Measured, even-handed bladework.",
                },
                Secondary = new SaberMove
                {
                    Name = "Force-Guided Strike", Donors = Sword, DonorSecondary = true,
                    Lore = "The blade and the Force move together, blasting foes back as the strike lands.",
                    ForcePushOnUse = true, Damage = 1.1f,
                },
            },

            // VII. Juyo / Vaapad: chaotic, bursting, deliberately unpredictable.
            new FormMoveset
            {
                PrimaryPool = new[]
                {
                    new SaberMove { Name = "Vornskr Frenzy", Donors = Sword, Damage = 1.05f },
                    new SaberMove { Name = "Vornskr Frenzy", Donors = DualKnives, Damage = 0.9f },
                    new SaberMove { Name = "Vornskr Frenzy", Donors = Axe, Damage = 1.1f },
                    new SaberMove { Name = "Vornskr Frenzy", Donors = Mace, Damage = 1.1f },
                    new SaberMove { Name = "Vornskr Frenzy", Donors = Greatsword, Damage = 1.0f },
                },
                Primary = new SaberMove
                {
                    Name = "Vornskr Frenzy", Donors = Sword,
                    Lore = "Every flurry takes a different shape, impossible to read.",
                },
                Secondary = new SaberMove
                {
                    Name = "Vaapad Whirlwind", Donors = Greatsword, DonorSecondary = true,
                    Lore = "A ferocious all-in burst channelling the darkness of the duel.",
                    Damage = 1.5f, Stagger = 1.3f, Stamina = 1.4f,
                },
            },
        };
    }
}
