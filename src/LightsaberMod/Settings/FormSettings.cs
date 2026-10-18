using System;
using BepInEx.Configuration;
using LightsaberMod.Forms;

namespace LightsaberMod.Settings
{
    /// <summary>
    /// Config for each form's core numbers. Applied live: change a value (in the file, with a
    /// config manager, or with <c>lightsaber set</c>) and it takes effect immediately.
    /// Server admins' values are synced to everyone.
    /// </summary>
    internal static class FormSettings
    {
        public static void Bind(ConfigFile config, object synced)
        {
            foreach (SaberForm form in SaberForm.All)
            {
                string s = "Form " + form.RomanNumeral + ": " + form.Name.Replace("/", "-");
                BindFloat(config, s, "DamageDealt", form.DamageDealt, v => form.DamageDealt = v, "Damage multiplier (1 = normal).", synced);
                BindFloat(config, s, "DamageTaken", form.DamageTaken, v => form.DamageTaken = v, "Incoming damage multiplier.", synced);
                BindFloat(config, s, "AttackSpeed", form.AttackSpeed, v => form.AttackSpeed = v, "Attack animation speed.", synced);
                BindFloat(config, s, "AttackStamina", form.AttackStamina, v => form.AttackStamina = v, "Stamina cost per swing multiplier.", synced);
                BindFloat(config, s, "Stagger", form.Stagger, v => form.Stagger = v, "Stagger multiplier.", synced);
                BindFloat(config, s, "ForcePower", form.ForcePower, v => form.ForcePower = v, "Force power strength multiplier.", synced);
                BindFloat(config, s, "ForceCost", form.ForceCost, v => form.ForceCost = v, "Force power stamina cost multiplier.", synced);
                BindFloat(config, s, "MoveSpeed", form.MoveSpeed, v => { form.MoveSpeed = v; FormManager.RefreshStatusEffect(form); },
                    "Movement speed bonus (0.15 = +15%).", synced);
                BindFloat(config, s, "StaminaRegen", form.StaminaRegen, v => { form.StaminaRegen = v; FormManager.RefreshStatusEffect(form); },
                    "Stamina regeneration multiplier.", synced);
            }
        }

        private static void BindFloat(ConfigFile config, string section, string key, float value, Action<float> apply, string description, object synced)
        {
            ConfigEntry<float> entry = config.Bind(section, key, value, new ConfigDescription(description, null, synced));
            apply(entry.Value);
            entry.SettingChanged += (_, __) => apply(entry.Value);
        }
    }
}
