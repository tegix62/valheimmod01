using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using LightsaberMod.Forms;
using LightsaberMod.Items;
using UnityEngine;

namespace LightsaberMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class LightsaberPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "tegix62.LightsaberMod";
        public const string PluginName = "LightsaberMod";
        public const string PluginVersion = "0.1.0";

        public static ConfigEntry<float> DamageScale;
        public static ConfigEntry<bool> UseDurability;
        public static ConfigEntry<float> UnlockSkillStep;
        public static ConfigEntry<float> LifestealCap;
        public static ConfigEntry<float> ForceStaminaCost;
        public static ConfigEntry<float> ForceCooldown;
        public static ConfigEntry<int> SavedForm;
        public static ConfigEntry<bool> UseFormMovesets;
        public static ConfigEntry<float> SoundVolume;
        public static ConfigEntry<bool> ShowTrails;
        public static ConfigEntry<float> ThrowStaminaCost;
        public static ConfigEntry<float> ThrowRange;
        public static ConfigEntry<float> LightningDamage;
        public static ConfigEntry<float> LightningStaminaPerSecond;
        public static ConfigEntry<float> LeapStaminaCost;

        private ConfigEntry<KeyboardShortcut> _nextFormKey;
        private ConfigEntry<KeyboardShortcut> _prevFormKey;
        private ConfigEntry<KeyboardShortcut> _forcePushKey;
        private ConfigEntry<KeyboardShortcut> _forcePullKey;
        private ConfigEntry<KeyboardShortcut> _throwKey;
        private ConfigEntry<KeyboardShortcut> _lightningKey;

        private Harmony _harmony;

        private void Awake()
        {
            BindConfig();
            FormManager.Init(SavedForm.Value);
            SaberFactory.AddTranslations(LocalizationManager.Instance.GetLocalization());

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(LightsaberPlugin).Assembly);

            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            ItemManager.OnItemsRegistered += OnItemsRegistered;

            Jotunn.Logger.LogInfo($"{PluginName} {PluginVersion} loaded. May the Force be with you.");
        }

        private void BindConfig()
        {
            // Form and Force settings are read live, so the server's values win once connected.
            var synced = new ConfigurationManagerAttributes { IsAdminOnly = true };

            // Item stats are baked into the prefabs at startup, before any server sync could
            // arrive, so these stay local. Keep them identical across players.
            DamageScale = Config.Bind("Balance", "DamageScale", 1f,
                new ConfigDescription("Multiplier on all lightsaber base damage. Restart required.", new AcceptableValueRange<float>(0.1f, 5f)));
            UseDurability = Config.Bind("Balance", "UseDurability", false,
                "If false, lightsabers never wear out. Restart required.");
            UnlockSkillStep = Config.Bind("Forms", "UnlockSkillStep", 5f,
                new ConfigDescription("Form N unlocks at weapon skill (N-1) x this value. 0 unlocks all forms immediately.", new AcceptableValueRange<float>(0f, 15f), synced));
            UseFormMovesets = Config.Bind("Forms", "UseFormMovesets", true,
                "Give each form its own attacks, borrowed from vanilla weapon animations. Disable to keep each saber's base moveset. Restart required.");
            LifestealCap = Config.Bind("Forms", "VaapadLifestealCap", 12f,
                new ConfigDescription("Maximum health Juyo/Vaapad can restore from a single hit.", new AcceptableValueRange<float>(0f, 100f), synced));
            ForceStaminaCost = Config.Bind("Force", "StaminaCost", 25f,
                new ConfigDescription("Stamina cost of Force Push/Pull before form modifiers.", new AcceptableValueRange<float>(0f, 100f), synced));
            ForceCooldown = Config.Bind("Force", "Cooldown", 4f,
                new ConfigDescription("Seconds between Force powers.", new AcceptableValueRange<float>(0f, 60f), synced));

            ThrowStaminaCost = Config.Bind("Saber Throw", "StaminaCost", 20f,
                new ConfigDescription("Stamina cost of throwing your lightsaber.", new AcceptableValueRange<float>(0f, 100f), synced));
            ThrowRange = Config.Bind("Saber Throw", "Range", 14f,
                new ConfigDescription("How far the thrown saber flies before returning, in metres.", new AcceptableValueRange<float>(4f, 30f), synced));

            LightningDamage = Config.Bind("Force", "LightningDamage", 6f,
                new ConfigDescription("Lightning damage per tick (5 ticks a second) to the first target, before form modifiers.", new AcceptableValueRange<float>(0f, 50f), synced));
            LightningStaminaPerSecond = Config.Bind("Force", "LightningStaminaPerSecond", 15f,
                new ConfigDescription("Stamina drained per second while channelling Force lightning.", new AcceptableValueRange<float>(0f, 100f), synced));
            LeapStaminaCost = Config.Bind("Force", "LeapStaminaCost", 15f,
                new ConfigDescription("Stamina cost of a Force Leap (press jump in mid-air).", new AcceptableValueRange<float>(0f, 100f), synced));

            SoundVolume = Config.Bind("Effects", "SoundVolume", 0.6f,
                new ConfigDescription("Volume of lightsaber hum, ignition and clash sounds. 0 mutes them.", new AcceptableValueRange<float>(0f, 1f)));
            ShowTrails = Config.Bind("Effects", "ShowTrails", true, "Draw glowing trails behind fast swings.");

            _nextFormKey = Config.Bind("Controls", "NextForm", new KeyboardShortcut(KeyCode.H), "Switch to the next lightsaber form.");
            _prevFormKey = Config.Bind("Controls", "PreviousForm", new KeyboardShortcut(KeyCode.H, KeyCode.LeftShift), "Switch to the previous lightsaber form.");
            _forcePushKey = Config.Bind("Controls", "ForcePush", new KeyboardShortcut(KeyCode.G), "Force Push the enemies in front of you.");
            _forcePullKey = Config.Bind("Controls", "ForcePull", new KeyboardShortcut(KeyCode.G, KeyCode.LeftShift), "Force Pull the enemies in front of you.");

            _lightningKey = Config.Bind("Controls", "ForceLightning", new KeyboardShortcut(KeyCode.R), "Hold to channel Force lightning.");
            _throwKey = Config.Bind("Controls", "SaberThrow", new KeyboardShortcut(KeyCode.T), "Throw your lightsaber; it returns to your hand.");

            SavedForm = Config.Bind("State", "LastForm", 0, "Last selected form (0 = Shii-Cho ... 6 = Juyo). Managed by the mod.");
        }

        private void OnVanillaPrefabsAvailable()
        {
            SaberFactory.RegisterItems();
            MovesetManager.Build();
            FormManager.RegisterStatusEffects();
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private void OnItemsRegistered()
        {
            SaberFactory.RenderIcons();
            ItemManager.OnItemsRegistered -= OnItemsRegistered;
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                ForceLightning.Stop();
                return;
            }

            FormManager.Tick(player);

            bool canInput = player.TakeInput();
            ForceLeap.Update(player, canInput);
            if (!canInput)
            {
                ForceLightning.Stop();
                return;
            }

            ForceLightning.Update(player, _lightningKey.Value.IsPressed());

            // Shifted variants first, so rebinding either key to a plain one can't fire both.
            if (_prevFormKey.Value.IsDown()) FormManager.Cycle(player, -1);
            else if (_nextFormKey.Value.IsDown()) FormManager.Cycle(player, +1);

            if (_forcePullKey.Value.IsDown()) ForcePowers.Pull(player);
            else if (_forcePushKey.Value.IsDown()) ForcePowers.Push(player);

            if (_throwKey.Value.IsDown()) SaberThrow.Throw(player);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
