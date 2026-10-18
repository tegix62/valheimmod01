namespace LightsaberMod.Items
{
    /// <summary>
    /// A lightsaber chassis. Each one borrows a vanilla weapon for its animations, skill and
    /// attack chain, and replaces the visible model with a glowing blade.
    /// </summary>
    internal sealed class HiltType
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string BasePrefab;      // vanilla weapon whose moveset we inherit
        public float BladeLength;      // metres, per blade
        public float HiltLength;
        public bool DoubleBladed;
        public float Slash;
        public float Fire;             // plasma burns
        public float SlashPerLevel;
        public float FirePerLevel;
        public float BlockPower;
        public float Deflection;
        public float ParryBonus;       // timed block multiplier
        public int Hilts;              // emitter hilts consumed
        public int Crystals;           // kyber crystals consumed

        public static readonly HiltType[] All =
        {
            new HiltType
            {
                Id = "saber", DisplayName = "Lightsaber",
                Description = "An elegant weapon for a more civilised age. Uses the Swords skill.",
                BasePrefab = "SwordIron",
                BladeLength = 1.05f, HiltLength = 0.26f,
                Slash = 55, Fire = 25, SlashPerLevel = 6, FirePerLevel = 3,
                BlockPower = 40, Deflection = 40, ParryBonus = 2.5f,
                Hilts = 1, Crystals = 1,
            },
            new HiltType
            {
                Id = "staff", DisplayName = "Saberstaff",
                Description = "Two blades on one long hilt, built for sweeping crowds. Uses the Polearms skill.",
                BasePrefab = "AtgeirIron",
                BladeLength = 0.95f, HiltLength = 0.6f, DoubleBladed = true,
                Slash = 50, Fire = 22, SlashPerLevel = 5, FirePerLevel = 3,
                BlockPower = 45, Deflection = 45, ParryBonus = 2f,
                Hilts = 2, Crystals = 2,
            },
            new HiltType
            {
                Id = "shoto", DisplayName = "Shoto",
                Description = "A short-bladed lightsaber, quick in close quarters. Uses the Knives skill.",
                BasePrefab = "KnifeSilver",
                BladeLength = 0.55f, HiltLength = 0.18f,
                Slash = 34, Fire = 16, SlashPerLevel = 4, FirePerLevel = 2,
                BlockPower = 20, Deflection = 20, ParryBonus = 3f,
                Hilts = 1, Crystals = 1,
            },
        };

        public string PrefabFor(KyberColor color) => "Lightsaber_" + Id + "_" + color.Id;

        // Every saber token shares the "item_ls_" prefix; SaberFactory.IsSaber relies on it.
        public string TokenFor(KyberColor color) => "item_ls_" + Id + "_" + color.Id;
    }
}
