using System.Collections.Generic;
using LightsaberMod.Items;
using LightsaberMod.Util;
using UnityEngine;

namespace LightsaberMod.Forms
{
    /// <summary>
    /// Throws the held lightsaber: it spins out along the aim, cuts everything it passes
    /// through, then curves back to the wielder's hand. While it's in flight the saber can't
    /// be swung.
    /// </summary>
    /// <remarks>
    /// Only the thrower's client deals damage. Every other client is told about the throw
    /// (see <see cref="Net.NetSync"/>) and flies a cosmetic copy along the same path.
    /// </remarks>
    internal sealed class SaberThrow : MonoBehaviour
    {
        private const float OutSpeed = 26f;
        private const float ReturnSpeed = 30f;
        private const float HitRadius = 0.75f;
        private const float SpinDegreesPerSecond = 1440f;

        private static SaberThrow _active;
        private static int _solidMask = -1;

        private const float RemoteTimeout = 6f;

        private Player _player;
        private bool _local;
        private float _spawnedAt;
        private ItemDrop.ItemData _weapon;
        private GameObject _handModel;
        private Vector3 _direction;
        private float _maxDistance;
        private float _travelled;
        private bool _returning;
        private float _damageScale;
        private readonly HashSet<Character> _hitThisLeg = new HashSet<Character>();

        public static bool InFlight => _active != null;

        public static void Throw(Player player)
        {
            if (_active != null) return;

            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (!SaberFactory.IsSaber(weapon))
            {
                player.Message(MessageHud.MessageType.Center, "Draw a lightsaber to throw it");
                return;
            }

            float cost = LightsaberPlugin.ThrowStaminaCost.Value;
            if (!player.HaveStamina(cost))
            {
                player.Message(MessageHud.MessageType.Center, "Not enough stamina");
                return;
            }

            GameObject prefabModel = weapon.m_dropPrefab != null ? weapon.m_dropPrefab.transform.Find("attach")?.gameObject : null;
            if (prefabModel == null) return;

            Compat.Call(player, "UseStamina", cost);

            Vector3 origin = player.GetCenterPoint() + player.transform.forward * 0.6f;
            Vector3 aim = (GameCamera.instance != null ? GameCamera.instance.transform.forward : player.transform.forward).normalized;
            float range = LightsaberPlugin.ThrowRange.Value;

            SaberThrow t = Spawn(player, prefabModel, origin, aim, range);
            t._local = true;
            t._weapon = weapon;
            t._damageScale = 0.8f * FormManager.Current.ThrowPower;
            _active = t;

            Net.NetSync.BroadcastThrow(player, weapon.m_dropPrefab.name, origin, aim, range);
            player.Message(MessageHud.MessageType.TopLeft, "Saber Throw");
        }

        /// <summary>Another player's throw, replayed for show: same path, no damage.</summary>
        public static void SpawnRemote(Player thrower, string prefabName, Vector3 origin, Vector3 aim, float range)
        {
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
            GameObject model = prefab != null ? prefab.transform.Find("attach")?.gameObject : null;
            if (model != null) Spawn(thrower, model, origin, aim, range);
        }

        private static SaberThrow Spawn(Player player, GameObject prefabModel, Vector3 origin, Vector3 aim, float range)
        {
            // Instantiate the model's root rather than the prefab, so no ItemDrop/ZNetView comes along.
            GameObject flying = Instantiate(prefabModel, origin, Quaternion.identity);
            flying.name = "thrown_lightsaber";
            foreach (Collider col in flying.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(col); // vanilla colliders would block the sweep tests below

            // Lay the blade flat along the aim; it then spins about the world vertical, Vader-style.
            Transform model = flying.transform.Find("lightsaber_model");
            Vector3 flat = Vector3.ProjectOnPlane(aim, Vector3.up);
            if (model != null && flat.sqrMagnitude > 0.001f)
                flying.transform.rotation = Quaternion.FromToRotation(model.up, flat.normalized) * flying.transform.rotation;
            SaberThrow t = flying.AddComponent<SaberThrow>();
            t._player = player;
            t._direction = aim;
            t._maxDistance = range;
            t._spawnedAt = Time.time;
            t._handModel = Compat.GetField<GameObject>(player.GetComponent<VisEquipment>(), "m_rightItemInstance");
            if (t._handModel != null) t._handModel.SetActive(false);

            SaberAudio.PlayAt(SaberAudio.Ignite, origin, 0.6f, 1.3f);
            return t;
        }

        private void Awake()
        {
            // The model was built for a hand: start it lit with its hum running even though nobody holds it.
            AudioSource hum = gameObject.AddComponent<AudioSource>();
            SaberAudio.Configure(hum);
            hum.clip = SaberAudio.Hum;
            hum.loop = true;
            hum.pitch = 1.5f;
            hum.volume = SaberAudio.Volume;
            hum.Play();
            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
                trail.emitting = LightsaberPlugin.ShowTrails.Value;
        }

        private void Update()
        {
            if (_player == null || _player.IsDead() || (_local
                    ? _player.GetCurrentWeapon() != _weapon
                    : Time.time - _spawnedAt > RemoteTimeout)) // never strand a replayed throw
            {
                // Wielder died, swapped weapons or left: the throw just ends.
                Finish();
                return;
            }

            float dt = Time.deltaTime;
            transform.Rotate(Vector3.up, SpinDegreesPerSecond * dt, Space.World);

            Vector3 from = transform.position;
            Vector3 to;
            if (!_returning)
            {
                to = from + _direction * OutSpeed * dt;
                _travelled += OutSpeed * dt;
                if (_travelled >= _maxDistance || HitsSolid(from, to))
                    StartReturn();
            }
            else
            {
                Vector3 hand = _player.GetCenterPoint();
                to = Vector3.MoveTowards(from, hand, ReturnSpeed * dt);
                if (Vector3.Distance(to, hand) < 0.8f)
                {
                    Finish();
                    return;
                }
            }

            transform.position = to;
            if (_local) HitCharacters(to);
        }

        private void StartReturn()
        {
            _returning = true;
            _hitThisLeg.Clear(); // the way back can cut the same enemies again
        }

        private bool HitsSolid(Vector3 from, Vector3 to)
        {
            if (_solidMask == -1)
                _solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            return Physics.Linecast(from, to, _solidMask, QueryTriggerInteraction.Ignore);
        }

        private void HitCharacters(Vector3 at)
        {
            foreach (Collider c in Physics.OverlapSphere(at, HitRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                Character target = c.GetComponentInParent<Character>();
                if (target == null || target == _player || target.IsDead() || _hitThisLeg.Contains(target)) continue;
                if (!BaseAI.IsEnemy(_player, target)) continue;
                _hitThisLeg.Add(target);

                ItemDrop.ItemData.SharedData shared = _weapon.m_shared;
                int levels = Mathf.Max(0, _weapon.m_quality - 1);
                var hit = new HitData();
                hit.m_damage.m_slash = (shared.m_damages.m_slash + shared.m_damagesPerLevel.m_slash * levels) * _damageScale;
                hit.m_damage.m_fire = (shared.m_damages.m_fire + shared.m_damagesPerLevel.m_fire * levels) * _damageScale;
                hit.m_point = target.GetCenterPoint();
                hit.m_dir = (target.GetCenterPoint() - _player.GetCenterPoint()).normalized;
                hit.m_pushForce = 25f;
                hit.m_staggerMultiplier = 1.5f;
                hit.SetAttacker(_player);

                ForcePowers.Channeling = true; // throw damage is already scaled by the form's ThrowPower
                try { target.Damage(hit); }
                finally { ForcePowers.Channeling = false; }

                SaberAudio.PlayAt(SaberAudio.Clash, hit.m_point, 0.5f, 1.4f);
            }
        }

        private void Finish()
        {
            if (_handModel != null) _handModel.SetActive(true); // re-enabling replays the ignition: the catch
            if (_active == this) _active = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_active == this)
            {
                _active = null;
                if (_handModel != null) _handModel.SetActive(true);
            }
        }
    }
}
