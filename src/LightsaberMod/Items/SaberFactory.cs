using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace LightsaberMod.Items
{
    /// <summary>
    /// Registers kyber crystals, the emitter hilt and every hilt/colour combination, and swaps
    /// the cloned vanilla models for procedurally built hilts and glowing blades.
    /// </summary>
    internal static class SaberFactory
    {
        public const string HiltPrefab = "LightsaberEmitterHilt";
        private const string SaberTokenPrefix = "$item_ls_";

        private static Material _hiltMaterial;
        private static Material _gripMaterial;
        private static readonly Dictionary<string, Material> BladeMaterials = new Dictionary<string, Material>();

        public static bool IsSaber(ItemDrop.ItemData item) =>
            item?.m_shared?.m_name != null && item.m_shared.m_name.StartsWith(SaberTokenPrefix);

        /// <summary>Item names and descriptions. Needs no prefabs, so it runs in Awake before the language loads.</summary>
        public static void AddTranslations(CustomLocalization loc)
        {
            var english = new Dictionary<string, string>
            {
                { "item_kyberhilt", "Emitter Hilt" },
                { "item_kyberhilt_desc", "A machined hilt with a focusing lens and power cell. Useless without a kyber crystal." },
            };

            foreach (KyberColor color in KyberColor.All)
            {
                english[color.CrystalToken] = color.DisplayName + " Kyber Crystal";
                english[color.CrystalToken + "_desc"] = "A living crystal attuned at the forge. " + color.Lore;
            }

            foreach (HiltType hilt in HiltType.All)
            foreach (KyberColor color in KyberColor.All)
            {
                string token = hilt.TokenFor(color);
                english[token] = color.Id == "black" && hilt.Id == "saber" ? "Darksaber" : color.DisplayName + " " + hilt.DisplayName;
                english[token + "_desc"] = hilt.Description + " " + color.Lore;
            }

            loc.AddTranslation("English", english);
        }

        public static void RegisterItems()
        {
            PowerScaling.Resolve();
            AddHilt();
            foreach (KyberColor color in KyberColor.All)
                AddCrystal(color);

            foreach (HiltType hilt in HiltType.All)
            foreach (KyberColor color in KyberColor.All)
                AddSaber(hilt, color);
        }

        private static void AddHilt()
        {
            var config = new ItemConfig
            {
                // Token deliberately avoids the saber prefix so a hilt in hand never counts as a saber.
                Name = "$item_kyberhilt",
                Description = "$item_kyberhilt_desc",
                // Mistlands tier: built at the Black Forge from refined eitr and a black core.
                CraftingStation = CraftingStations.BlackForge,
                MinStationLevel = 1,
                Requirements = new[]
                {
                    new RequirementConfig("BlackMetal", 4),
                    new RequirementConfig("Eitr", 2),
                    new RequirementConfig("BlackCore", 1),
                },
            };
            var item = new CustomItem(HiltPrefab, "Iron", config);
            ItemManager.Instance.AddItem(item);
        }

        private static void AddCrystal(KyberColor color)
        {
            var config = new ItemConfig
            {
                Name = "$" + color.CrystalToken,
                Description = "$" + color.CrystalToken + "_desc",
                CraftingStation = CraftingStations.Forge,
                MinStationLevel = 1,
                Requirements = color.Attunement.Select(i => new RequirementConfig(i.Item, i.Amount)).ToArray(),
            };

            var item = new CustomItem(color.CrystalPrefab, "Crystal", config);
            Tint(item.ItemPrefab, color.Id == "black" ? color.Glow : color.Blade);
            ItemManager.Instance.AddItem(item);
        }

        private static void AddSaber(HiltType hilt, KyberColor color)
        {
            string token = hilt.TokenFor(color);
            var config = new ItemConfig
            {
                Name = "$" + token,
                Description = "$" + token + "_desc",
                CraftingStation = CraftingStations.BlackForge,
                RepairStation = CraftingStations.BlackForge,
                MinStationLevel = 2,
                Requirements = new[]
                {
                    new RequirementConfig(HiltPrefab, hilt.Hilts, 0, true),
                    new RequirementConfig(color.CrystalPrefab, hilt.Crystals, 0, true),
                    new RequirementConfig("BlackMetal", 0, 4, true),
                    new RequirementConfig("Eitr", 0, 2, true),
                },
            };

            var item = new CustomItem(hilt.PrefabFor(color), hilt.BasePrefab, config);
            if (item.ItemDrop == null)
            {
                Logger.LogWarning($"Base prefab {hilt.BasePrefab} missing; skipping {hilt.PrefabFor(color)}");
                return;
            }

            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            PowerScaling.Apply(shared, hilt);
            shared.m_deflectionForce = hilt.Deflection;
            shared.m_timedBlockBonus = hilt.ParryBonus;
            shared.m_weight = hilt.DoubleBladed ? 1.6f : 0.9f;
            shared.m_maxQuality = 4;
            if (!LightsaberPlugin.UseDurability.Value)
                shared.m_useDurability = false;

            BuildSaberModel(item.ItemPrefab, hilt, color);
            ItemManager.Instance.AddItem(item);
        }

        /// <summary>Renders inventory icons from the finished models. Needs the Jotunn render pipeline, so runs after registration.</summary>
        public static void RenderIcons()
        {
            foreach (HiltType hilt in HiltType.All)
            foreach (KyberColor color in KyberColor.All)
            {
                CustomItem item = ItemManager.Instance.GetItem(hilt.PrefabFor(color));
                if (item?.ItemPrefab == null) continue;
                try
                {
                    Sprite icon = RenderManager.Instance.Render(item.ItemPrefab, RenderManager.IsometricRotation);
                    if (icon != null)
                        item.ItemDrop.m_itemData.m_shared.m_icons = new[] { icon };
                }
                catch (System.Exception e)
                {
                    Logger.LogDebug($"Icon render failed for {item.ItemPrefab.name}, keeping vanilla icon: {e.Message}");
                }
            }
        }

        // ----- Model building -------------------------------------------------------------

        private static void BuildSaberModel(GameObject prefab, HiltType hilt, KyberColor color)
        {
            // VisEquipment instantiates the "attach" child when the weapon is held; the ground
            // model is the same child, so one replacement covers both.
            Transform attach = prefab.transform.Find("attach") ?? prefab.transform;

            Vector3 axis = FindBladeAxis(attach);
            foreach (Renderer r in attach.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            foreach (LODGroup lod in attach.GetComponentsInChildren<LODGroup>(true))
                lod.enabled = false;

            var root = new GameObject("lightsaber_model");
            root.transform.SetParent(attach, false);
            root.transform.localRotation = Quaternion.FromToRotation(Vector3.up, axis);

            Material blade = BladeMaterial(color);
            if (hilt.DoubleBladed)
            {
                // Hilt centred on the grip, a blade out of each end.
                AddHiltPieces(root.transform, -hilt.HiltLength / 2f, hilt.HiltLength);
                AddBlade(root.transform, hilt.HiltLength / 2f, hilt.BladeLength, blade, color, +1);
                AddBlade(root.transform, -hilt.HiltLength / 2f, hilt.BladeLength, blade, color, -1);
            }
            else
            {
                // Grip sits slightly behind the hand so the emitter clears the fist.
                float start = -hilt.HiltLength * 0.35f;
                AddHiltPieces(root.transform, start, hilt.HiltLength);
                AddBlade(root.transform, start + hilt.HiltLength, hilt.BladeLength, blade, color, +1);
            }

            root.AddComponent<SaberBlade>();
        }

        /// <summary>
        /// Direction from the grip toward the tip of the vanilla weapon, in attach space: the
        /// longest axis of its mesh bounds, signed toward where most of the mesh is.
        /// </summary>
        private static Vector3 FindBladeAxis(Transform attach)
        {
            Bounds? bounds = null;
            foreach (MeshFilter mf in attach.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                Matrix4x4 m = attach.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                foreach (Vector3 corner in Corners(mb))
                {
                    Vector3 p = m.MultiplyPoint3x4(corner);
                    if (bounds == null) bounds = new Bounds(p, Vector3.zero);
                    else { Bounds b = bounds.Value; b.Encapsulate(p); bounds = b; }
                }
            }

            if (bounds == null) return Vector3.up;

            Vector3 size = bounds.Value.size, center = bounds.Value.center;
            if (size.x >= size.y && size.x >= size.z) return center.x >= 0 ? Vector3.right : Vector3.left;
            if (size.z >= size.y) return center.z >= 0 ? Vector3.forward : Vector3.back;
            return center.y >= 0 ? Vector3.up : Vector3.down;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (int i = 0; i < 8; i++)
                yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }

        private static void AddHiltPieces(Transform parent, float start, float length)
        {
            _hiltMaterial = _hiltMaterial ?? MakeMaterial(new Color(0.62f, 0.64f, 0.68f), 0.85f, Color.black);
            _gripMaterial = _gripMaterial ?? MakeMaterial(new Color(0.08f, 0.08f, 0.09f), 0.3f, Color.black);

            Cylinder(parent, "hilt_body", start, length, 0.036f, _hiltMaterial);
            // Grip ridges.
            for (int i = 0; i < 4; i++)
                Cylinder(parent, "hilt_grip" + i, start + length * (0.15f + i * 0.14f), length * 0.06f, 0.040f, _gripMaterial);
            // Emitter shroud(s).
            Cylinder(parent, "hilt_emitter", start + length - 0.03f, 0.03f, 0.044f, _hiltMaterial);
            Cylinder(parent, "hilt_pommel", start, 0.025f, 0.041f, _gripMaterial);
        }

        private static void AddBlade(Transform parent, float from, float length, Material mat, KyberColor color, int sign)
        {
            // Everything hangs off a pivot at the emitter, so SaberBlade can grow the blade out
            // of the hilt on ignition by scaling one transform.
            var pivot = new GameObject(SaberBlade.PivotName).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(0, from, 0);
            if (sign < 0) pivot.localRotation = Quaternion.Euler(180f, 0f, 0f);

            Cylinder(pivot, "blade", 0f, length, 0.022f, mat);

            if (color.Id == "black")
            {
                // The Darksaber's pale crackling edge.
                Cylinder(pivot, "blade_edge", 0f, length, 0.026f, MakeMaterial(color.Glow * 0.2f, 0.9f, color.Glow * 1.2f), 0.18f);
            }

            var lightObj = new GameObject("blade_light");
            lightObj.transform.SetParent(pivot, false);
            lightObj.transform.localPosition = new Vector3(0, length / 2f, 0);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color.Glow;
            light.range = 3.2f;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;

            // Trail from the inner third of the blade to the tip, so swings leave a band of light.
            var tip = new GameObject(SaberBlade.TipName);
            tip.transform.SetParent(pivot, false);
            tip.transform.localPosition = new Vector3(0, length * 0.65f, 0);
            TrailRenderer trail = tip.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.minVertexDistance = 0.04f;
            trail.widthMultiplier = length * 0.7f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f));
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.sharedMaterial = TrailMaterial();
            Color trailColor = color.Id == "black" ? color.Glow * 0.6f : color.Blade;
            trail.colorGradient = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.Lerp(trailColor, Color.white, 0.3f), 0f), new GradientColorKey(trailColor, 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) },
            };
        }

        private static Material _trailMaterial;

        private static Material TrailMaterial()
        {
            if (_trailMaterial != null) return _trailMaterial;
            Shader shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            _trailMaterial = new Material(shader);
            return _trailMaterial;
        }

        private static GameObject Cylinder(Transform parent, string name, float start, float length, float radius, Material mat, float flatten = 1f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            // Unity's cylinder is 2 units tall along Y, centred on the origin.
            go.transform.localPosition = new Vector3(0, start + length / 2f, 0);
            go.transform.localScale = new Vector3(radius * 2f, length / 2f, radius * 2f * flatten);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Material BladeMaterial(KyberColor color)
        {
            if (!BladeMaterials.TryGetValue(color.Id, out Material mat))
            {
                // Whitish core with a strong coloured emission reads as a glowing plasma blade.
                Color core = color.Id == "black" ? color.Blade : Color.Lerp(color.Blade, Color.white, 0.55f);
                Color emission = color.Id == "black" ? color.Glow * 0.08f : color.Blade * 4f;
                mat = MakeMaterial(core, 0.9f, emission);
                BladeMaterials[color.Id] = mat;
            }
            return mat;
        }

        private static Material MakeMaterial(Color color, float smoothness, Color emission)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", emission.maxColorComponent > 0 ? 0f : 0.8f);
            if (emission.maxColorComponent > 0 && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        private static void Tint(GameObject prefab, Color color)
        {
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var m = new Material(mats[i]);
                    if (m.HasProperty("_Color")) m.SetColor("_Color", color);
                    if (m.HasProperty("_EmissionColor"))
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", color * 1.5f);
                    }
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }

            var lightObj = new GameObject("kyber_glow");
            lightObj.transform.SetParent(prefab.transform, false);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 1.5f;
            light.intensity = 1f;
            light.shadows = LightShadows.None;
        }
    }
}
