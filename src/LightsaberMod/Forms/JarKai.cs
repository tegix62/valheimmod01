using System.Linq;
using LightsaberMod.Items;
using LightsaberMod.Net;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Jar'Kai, two-blade fighting built on Niman. Valheim's off hand only holds shields and
    /// torches, so the second saber isn't equipped. It's drawn from the inventory and shown in
    /// the left hand while these all hold:
    /// <list type="bullet">
    /// <item>the form is Niman,</item>
    /// <item>a one-handed lightsaber or shoto is in the main hand,</item>
    /// <item>nothing is in the off hand,</item>
    /// <item>another one-handed lightsaber or shoto is in the inventory.</item>
    /// </list>
    /// </summary>
    internal static class JarKai
    {
        /// <summary>Share of the off-hand blade's damage added to every hit as its follow-through cut.</summary>
        public const float OffhandDamageShare = 0.4f;
        public const float BlockPowerBonus = 1.3f;
        public const float AttackStaminaCost = 1.15f;

        private static float _nextCheck;
        private static Player _player;
        private static string _published;

        /// <summary>The second saber while Jar'Kai is active for the local player, else null.</summary>
        public static ItemDrop.ItemData Offhand { get; private set; }

        public static bool Active => Offhand != null;

        public static void Tick(Player player)
        {
            if (player != _player)
            {
                _player = player;
                _published = null;
                Offhand = null;
            }

            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.25f;

            Offhand = FindOffhand(player);

            string prefab = Offhand?.m_dropPrefab != null ? Offhand.m_dropPrefab.name : "";
            if (prefab != _published)
            {
                NetSync.PublishJarKai(player, prefab);
                if (_published != null && prefab.Length > 0)
                    player.Message(MessageHud.MessageType.TopLeft, "Jar'Kai: second blade drawn");
                _published = prefab;
            }
        }

        private static ItemDrop.ItemData FindOffhand(Player player)
        {
            if (!LightsaberPlugin.JarKaiEnabled.Value || !FormManager.Current.AllowsJarKai) return null;

            ItemDrop.ItemData main = player.GetCurrentWeapon();
            if (!IsOneHandedSaber(main)) return null;
            if (Compat.GetField<ItemDrop.ItemData>(player, "m_leftItem") != null) return null; // shield or torch in hand
            if (SaberThrow.InFlight) return null;

            // Prefer a shoto, the classic Jar'Kai partner, then any other saber; best quality first.
            return player.GetInventory().GetAllItems()
                .Where(i => i != main && IsOneHandedSaber(i))
                .OrderByDescending(i => i.m_dropPrefab.name.Contains("_shoto_"))
                .ThenByDescending(i => i.m_quality)
                .FirstOrDefault();
        }

        public static bool IsOneHandedSaber(ItemDrop.ItemData item) =>
            SaberFactory.IsSaber(item) && item.m_dropPrefab != null && !item.m_dropPrefab.name.Contains("_staff_");

        /// <summary>Off-hand follow-through damage for one hit.</summary>
        public static void AddOffhandCut(HitData hit)
        {
            ItemDrop.ItemData off = Offhand;
            if (off == null) return;
            int levels = Mathf.Max(0, off.m_quality - 1);
            ItemDrop.ItemData.SharedData s = off.m_shared;
            hit.m_damage.m_slash += (s.m_damages.m_slash + s.m_damagesPerLevel.m_slash * levels) * OffhandDamageShare;
            hit.m_damage.m_fire += (s.m_damages.m_fire + s.m_damagesPerLevel.m_fire * levels) * OffhandDamageShare;
        }
    }

    /// <summary>
    /// Shows a player's Jar'Kai off-hand saber in their left hand. Runs for every player, local
    /// and remote; remote players' choice of blade arrives through their ZDO.
    /// </summary>
    internal sealed class JarKaiVisual : MonoBehaviour
    {
        private Player _player;
        private VisEquipment _vis;
        private string _shown = "";
        private GameObject _instance;
        private float _nextCheck;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _vis = GetComponent<VisEquipment>();
        }

        private void Update()
        {
            if (Time.time < _nextCheck || _player == null) return;
            _nextCheck = Time.time + 0.2f;

            GameObject right = Compat.GetField<GameObject>(_vis, "m_rightItemInstance");
            bool drawn = right != null && right.activeInHierarchy && right.GetComponentInChildren<SaberBlade>(true) != null;

            string wanted = "";
            if (drawn && !_player.IsDead())
                wanted = _player == Player.m_localPlayer
                    ? (JarKai.Offhand?.m_dropPrefab != null ? JarKai.Offhand.m_dropPrefab.name : "")
                    : NetSync.ReadJarKai(_player);

            if (wanted == _shown && (wanted.Length == 0 || _instance != null)) return;

            if (_instance != null) Destroy(_instance);
            _instance = null;
            _shown = wanted;
            if (wanted.Length > 0) _instance = Attach(wanted, right);
        }

        private GameObject Attach(string prefabName, GameObject rightInstance)
        {
            Transform leftHand = Compat.GetField<Transform>(_vis, "m_leftHand");
            Transform rightHand = Compat.GetField<Transform>(_vis, "m_rightHand");
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
            Transform model = prefab != null ? prefab.transform.Find("attach") : null;
            if (leftHand == null || model == null) return null;

            GameObject go = Instantiate(model.gameObject, leftHand, false);
            go.name = "jarkai_offhand";
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                Destroy(c);

            // Mirror the main-hand grip. Humanoid rigs mirror bone axes left to right, so the
            // same local pose usually reads correctly; JarKaiGripRotation corrects it if not.
            if (rightInstance != null && rightHand != null && rightInstance.transform.parent == rightHand)
            {
                go.transform.localPosition = rightInstance.transform.localPosition;
                go.transform.localRotation = rightInstance.transform.localRotation;
            }
            go.transform.localRotation *= Quaternion.Euler(LightsaberPlugin.JarKaiGripRotation.Value);
            return go;
        }

        private void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }
    }
}
