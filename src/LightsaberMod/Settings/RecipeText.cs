using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LightsaberMod.Items;

namespace LightsaberMod.Settings
{
    /// <summary>Recipes as config text: <c>Item:amount, Item:amount</c>, e.g. <c>BlackMetal:4, Eitr:2</c>.</summary>
    internal static class RecipeText
    {
        public static string Format(IEnumerable<Ingredient> ingredients) =>
            string.Join(", ", ingredients.Select(i => i.Item + ":" + i.Amount.ToString(CultureInfo.InvariantCulture)));

        public static Ingredient[] Parse(string text, Ingredient[] fallback, string where)
        {
            var result = new List<Ingredient>();
            foreach (string part in (text ?? "").Split(','))
            {
                string p = part.Trim();
                if (p.Length == 0) continue;
                string[] kv = p.Split(':');
                if (kv.Length != 2 || kv[0].Trim().Length == 0
                    || !int.TryParse(kv[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0)
                {
                    Jotunn.Logger.LogWarning($"{where}: couldn't read '{p}' (expected Item:amount); using the default recipe");
                    return fallback;
                }
                result.Add(new Ingredient(kv[0].Trim(), amount));
            }
            return result.Count > 0 ? result.ToArray() : fallback;
        }
    }
}
