using UnityEngine;

namespace LightsaberMod.Items
{
    internal readonly struct Ingredient
    {
        public readonly string Item;
        public readonly int Amount;
        public Ingredient(string item, int amount) { Item = item; Amount = amount; }
    }

    /// <summary>A kyber crystal colour and what it takes to attune one at the forge.</summary>
    internal sealed class KyberColor
    {
        public string Id;            // used in prefab names and tokens: "blue"
        public string DisplayName;   // "Blue"
        public string Lore;
        public Color Blade;
        public Color Glow;           // colour of the point light (differs from the blade for the Darksaber)
        public Ingredient[] Attunement;
        public string Station = "Forge";
        public int StationLevel = 1;

        public static readonly KyberColor[] All =
        {
            new KyberColor
            {
                Id = "blue", DisplayName = "Blue",
                Lore = "Favoured by Guardians who meet danger head-on.",
                Blade = new Color(0.20f, 0.45f, 1.00f), Glow = new Color(0.30f, 0.55f, 1.00f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Blueberries", 5) },
            },
            new KyberColor
            {
                Id = "green", DisplayName = "Green",
                Lore = "Favoured by Consulars who seek wisdom before battle.",
                Blade = new Color(0.20f, 1.00f, 0.25f), Glow = new Color(0.30f, 1.00f, 0.35f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Guck", 2) },
            },
            new KyberColor
            {
                Id = "yellow", DisplayName = "Yellow",
                Lore = "Carried by Sentinels who walk between the light and the wild.",
                Blade = new Color(1.00f, 0.85f, 0.15f), Glow = new Color(1.00f, 0.85f, 0.30f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Dandelion", 5) },
            },
            new KyberColor
            {
                Id = "purple", DisplayName = "Purple",
                Lore = "A rare hue for those who channel their darkness without surrendering to it.",
                Blade = new Color(0.65f, 0.20f, 1.00f), Glow = new Color(0.70f, 0.30f, 1.00f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Thistle", 5) },
            },
            new KyberColor
            {
                Id = "red", DisplayName = "Red",
                Lore = "A crystal bled by rage. It screams faintly when ignited.",
                Blade = new Color(1.00f, 0.08f, 0.05f), Glow = new Color(1.00f, 0.15f, 0.10f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Bloodbag", 3), new Ingredient("SurtlingCore", 1) },
            },
            new KyberColor
            {
                Id = "white", DisplayName = "White",
                Lore = "A bled crystal, healed and purified by the spirits of the dead.",
                Blade = new Color(0.95f, 0.97f, 1.00f), Glow = new Color(0.90f, 0.95f, 1.00f),
                // Purified from a red crystal (registered after red, see SaberFactory).
                Attunement = new[] { new Ingredient("KyberCrystal_red", 1), new Ingredient("Ectoplasm", 3) },
            },
            new KyberColor
            {
                Id = "black", DisplayName = "Dark",
                Lore = "An ancient, flat-edged blade that drinks the light around it.",
                Blade = new Color(0.03f, 0.03f, 0.05f), Glow = new Color(0.85f, 0.90f, 1.00f),
                Attunement = new[] { new Ingredient("Crystal", 1), new Ingredient("Obsidian", 5), new Ingredient("SurtlingCore", 2) },
            },
        };

        public string CrystalPrefab => "KyberCrystal_" + Id;
        public string CrystalToken => "item_kyber_" + Id;
    }
}
