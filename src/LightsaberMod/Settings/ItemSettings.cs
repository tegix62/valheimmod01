using BepInEx.Configuration;
using LightsaberMod.Items;
using UnityEngine;

namespace LightsaberMod.Settings
{
    /// <summary>
    /// Config for every item: each saber type's power, blade and recipe, each crystal's colours
    /// and recipe, and the emitter hilt. Read once at startup, before the items are built, so
    /// changes need a restart. Keep these identical for everyone in a multiplayer game.
    /// </summary>
    internal static class ItemSettings
    {
        public static Ingredient[] HiltRecipe = { new Ingredient("BlackMetal", 4), new Ingredient("Eitr", 2), new Ingredient("BlackCore", 1) };
        public static string HiltStation = "BlackForge";
        public static int HiltStationLevel = 1;

        private const string Restart = " Restart required.";
        private const string Stations = "Crafting station: Workbench, Forge, Stonecutter, Cauldron, ArtisanTable, BlackForge, GaldrTable, MeadKetill or FoodPreparationTable (or a station's prefab name).";

        public static void Bind(ConfigFile config)
        {
            const string hiltSection = "Item: Emitter Hilt";
            HiltRecipe = RecipeText.Parse(config.Bind(hiltSection, "Recipe", RecipeText.Format(HiltRecipe),
                "Ingredients as Item:amount, comma separated (vanilla prefab names)." + Restart).Value, HiltRecipe, hiltSection);
            HiltStation = config.Bind(hiltSection, "Station", HiltStation,
                Stations + Restart).Value;
            HiltStationLevel = config.Bind(hiltSection, "StationLevel", HiltStationLevel, "Minimum station level." + Restart).Value;

            foreach (HiltType hilt in HiltType.All)
            {
                string s = "Item: " + hilt.DisplayName;
                hilt.PowerFactor = config.Bind(s, "PowerFactor", hilt.PowerFactor,
                    "Damage relative to the reference weapon (Balance.PowerReference). 1 = equal." + Restart).Value;
                hilt.BlockFactor = config.Bind(s, "BlockFactor", hilt.BlockFactor, "Block power relative to the reference weapon." + Restart).Value;
                hilt.BladeLength = config.Bind(s, "BladeLength", hilt.BladeLength, "Blade length in metres (per blade)." + Restart).Value;
                hilt.Hilts = config.Bind(s, "HiltsNeeded", hilt.Hilts, "Emitter hilts per craft." + Restart).Value;
                hilt.Crystals = config.Bind(s, "CrystalsNeeded", hilt.Crystals, "Kyber crystals per craft." + Restart).Value;
                hilt.UpgradeCost = RecipeText.Parse(config.Bind(s, "UpgradeCostPerLevel", RecipeText.Format(hilt.UpgradeCost),
                    "Extra ingredients per quality level, as Item:amount." + Restart).Value, hilt.UpgradeCost, s);
                hilt.Station = config.Bind(s, "Station", hilt.Station, Stations + " Also used for repairs." + Restart).Value;
                hilt.StationLevel = config.Bind(s, "StationLevel", hilt.StationLevel, "Minimum station level." + Restart).Value;
            }

            foreach (KyberColor color in KyberColor.All)
            {
                string s = "Item: " + color.DisplayName + " Kyber Crystal";
                color.Attunement = RecipeText.Parse(config.Bind(s, "Recipe", RecipeText.Format(color.Attunement),
                    "Ingredients as Item:amount." + Restart).Value, color.Attunement, s);
                color.Station = config.Bind(s, "Station", color.Station, Stations + Restart).Value;
                color.StationLevel = config.Bind(s, "StationLevel", color.StationLevel, "Minimum station level." + Restart).Value;
                color.Blade = config.Bind(s, "BladeColor", color.Blade, "Blade colour (hex RRGGBBAA)." + Restart).Value;
                color.Glow = config.Bind(s, "GlowColor", color.Glow, "Colour of the light the blade casts (hex RRGGBBAA)." + Restart).Value;
            }
        }
    }
}
