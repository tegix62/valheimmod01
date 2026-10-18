using Jotunn.Managers;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Items
{
    /// <summary>
    /// Ties lightsaber damage to a vanilla weapon read from the game at startup (Mistwalker by
    /// default), so sabers sit at Mistlands strength and stay there across game patches.
    /// Upgrades add a share of that per level, reaching about early-Ashlands power at quality 4.
    /// </summary>
    internal static class PowerScaling
    {
        /// <summary>Share of saber damage dealt as fire. Kept low because much of the Ashlands resists fire.</summary>
        public const float FireShare = 0.2f;

        // Used if the configured reference and Blackmetal sword are both missing.
        private const float FallbackDamage = 110f;
        private const float FallbackBlock = 60f;

        public static float ReferenceDamage { get; private set; } = FallbackDamage;
        public static float ReferenceBlock { get; private set; } = FallbackBlock;

        public static void Resolve()
        {
            foreach (string name in new[] { LightsaberPlugin.PowerReference.Value, "SwordMistwalker", "SwordBlackmetal" })
            {
                ItemDrop.ItemData.SharedData shared = PrefabManager.Instance.GetPrefab(name)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
                float total = shared != null ? Total(shared.m_damages) : 0f;
                if (total <= 0f) continue;

                ReferenceDamage = total;
                ReferenceBlock = shared.m_blockPower > 0f ? shared.m_blockPower : FallbackBlock;
                Logger.LogInfo($"Lightsaber power matched to {name}: {ReferenceDamage:0} damage, {ReferenceBlock:0} block power");
                return;
            }
            Logger.LogWarning($"No reference weapon found; lightsabers use {FallbackDamage} damage");
        }

        /// <summary>Combat damage of all types (excludes chop and pickaxe).</summary>
        public static float Total(HitData.DamageTypes d) =>
            d.m_damage + d.m_blunt + d.m_slash + d.m_pierce + d.m_fire + d.m_frost + d.m_lightning + d.m_poison + d.m_spirit;

        /// <summary>An item's combat damage at its current quality.</summary>
        public static float CurrentTotal(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null) return 0f;
            int levels = UnityEngine.Mathf.Max(0, item.m_quality - 1);
            return Total(item.m_shared.m_damages) + Total(item.m_shared.m_damagesPerLevel) * levels;
        }

        public static void Apply(ItemDrop.ItemData.SharedData shared, HiltType hilt)
        {
            float total = ReferenceDamage * hilt.PowerFactor * LightsaberPlugin.DamageScale.Value;
            float perLevel = total * LightsaberPlugin.UpgradeScaling.Value;
            shared.m_damages = Split(total);
            shared.m_damagesPerLevel = Split(perLevel);
            shared.m_blockPower = ReferenceBlock * hilt.BlockFactor;
            shared.m_blockPowerPerLevel = shared.m_blockPower * LightsaberPlugin.UpgradeScaling.Value;
        }

        private static HitData.DamageTypes Split(float total) =>
            new HitData.DamageTypes { m_slash = total * (1f - FireShare), m_fire = total * FireShare };
    }
}
