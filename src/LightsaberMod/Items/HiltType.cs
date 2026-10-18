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
        public float PowerFactor;      // damage relative to the reference weapon (see PowerScaling)
        public float BlockFactor;      // block power relative to the reference weapon
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
                PowerFactor = 1f, BlockFactor = 1f,
                Deflection = 40, ParryBonus = 2.5f,
                Hilts = 1, Crystals = 1,
            },
            new HiltType
            {
                Id = "staff", DisplayName = "Saberstaff",
                Description = "Two blades on one long hilt, built for sweeping crowds. Uses the Polearms skill.",
                BasePrefab = "AtgeirIron",
                BladeLength = 0.95f, HiltLength = 0.6f, DoubleBladed = true,
                PowerFactor = 1f, BlockFactor = 1.1f,
                Deflection = 45, ParryBonus = 2f,
                Hilts = 2, Crystals = 2,
            },
            new HiltType
            {
                Id = "shoto", DisplayName = "Shoto",
                Description = "A short-bladed lightsaber, quick in close quarters. Uses the Knives skill.",
                BasePrefab = "KnifeSilver",
                BladeLength = 0.55f, HiltLength = 0.18f,
                PowerFactor = 0.55f, BlockFactor = 0.5f,
                Deflection = 20, ParryBonus = 3f,
                Hilts = 1, Crystals = 1,
            },
        };

        public string PrefabFor(KyberColor color) => "Lightsaber_" + Id + "_" + color.Id;

        // Every saber token shares the "item_ls_" prefix; SaberFactory.IsSaber relies on it.
        public string TokenFor(KyberColor color) => "item_ls_" + Id + "_" + color.Id;
    }
}
