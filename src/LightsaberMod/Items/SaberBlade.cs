using System.Collections.Generic;
using UnityEngine;

namespace LightsaberMod.Items
{
    /// <summary>
    /// Lives on every lightsaber model. When the model is held (by anyone, local or remote) it
    /// ignites the blade out of the hilt, hums with a pitch that follows the swing, draws
    /// trails on fast swings and plays the retract sound when sheathed.
    /// </summary>
    /// <remarks>
    /// Children are found by name in Awake rather than through serialized references, which
    /// is the safest option for a MonoBehaviour from a runtime-loaded plugin assembly.
    /// </remarks>
    internal sealed class SaberBlade : MonoBehaviour
    {
        public const string PivotName = "blade_pivot";
        public const string TipName = "blade_tip";

        private const float IgniteSeconds = 0.15f;
        private const float TrailSpeed = 5f;      // m/s of blade-tip speed relative to the wielder

        private readonly List<Transform> _pivots = new List<Transform>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<float> _lightIntensity = new List<float>();
        private readonly List<TrailRenderer> _trails = new List<TrailRenderer>();
        private Transform _tip;
        private AudioSource _hum;
        private Character _owner;
        private float _extended = 1f;
        private Vector3 _lastTip, _lastOwner;
        private float _swingSpeed;
        private int _ownerChecksLeft;

        private void Awake()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == PivotName) _pivots.Add(t);
                else if (t.name == TipName && _tip == null) _tip = t;
            }
            foreach (Light l in GetComponentsInChildren<Light>(true))
            {
                _lights.Add(l);
                _lightIntensity.Add(l.intensity);
            }
            _trails.AddRange(GetComponentsInChildren<TrailRenderer>(true));
        }

        private void OnEnable()
        {
            _owner = null;
            SetTrails(false);
            // Lying on the ground, on an item stand or in an icon render: lit, silent, still,
            // until (and unless) a hand turns up above us.
            SetExtended(1f);
            // The model may be instantiated before it's parented to a hand, so keep looking
            // for an owner for a few frames rather than deciding once.
            _ownerChecksLeft = 5;
            TryFindOwner();
        }

        private void TryFindOwner()
        {
            _ownerChecksLeft--;
            _owner = GetComponentInParent<Character>();
            if (_owner != null) Ignite();
        }

        private void Ignite()
        {
            SetExtended(0f);
            if (_tip != null) _lastTip = _tip.position;
            _lastOwner = _owner.transform.position;
            SaberAudio.PlayAt(SaberAudio.Ignite, transform.position, 0.9f);

            if (_hum == null)
            {
                _hum = gameObject.AddComponent<AudioSource>();
                SaberAudio.Configure(_hum);
                _hum.clip = SaberAudio.Hum;
                _hum.loop = true;
            }
            _hum.volume = 0f;
            _hum.Play();
        }

        private void OnDestroy()
        {
            // VisEquipment destroys the model on unequip; a dying owner or a scene teardown shouldn't make noise.
            if (_owner != null && !_owner.IsDead() && gameObject.scene.isLoaded)
                SaberAudio.PlayAt(SaberAudio.Retract, transform.position, 0.8f);
        }

        private void LateUpdate()
        {
            if (_owner == null)
            {
                if (_ownerChecksLeft > 0) TryFindOwner();
                if (_owner == null) return;
            }

            if (_extended < 1f)
                SetExtended(Mathf.MoveTowards(_extended, 1f, Time.deltaTime / IgniteSeconds));

            if (_tip != null && Time.deltaTime > 0f)
            {
                // Tip speed relative to the body, so running alone doesn't count as a swing.
                Vector3 ownerPos = _owner.transform.position;
                Vector3 relative = (_tip.position - _lastTip) - (ownerPos - _lastOwner);
                _lastTip = _tip.position;
                _lastOwner = ownerPos;
                _swingSpeed = Mathf.Lerp(_swingSpeed, relative.magnitude / Time.deltaTime, 0.35f);
            }

            SetTrails(LightsaberPlugin.ShowTrails.Value && _extended >= 1f && _swingSpeed > TrailSpeed);

            if (_hum != null)
            {
                float swing = Mathf.Clamp01(_swingSpeed / 14f);
                _hum.pitch = 1f + swing * 0.55f;
                _hum.volume = SaberAudio.Volume * _extended * (0.35f + swing * 0.65f);
            }
        }

        private void SetExtended(float amount)
        {
            _extended = amount;
            float eased = 1f - (1f - amount) * (1f - amount);
            foreach (Transform p in _pivots)
            {
                Vector3 s = p.localScale;
                p.localScale = new Vector3(s.x, Mathf.Max(0.001f, eased), s.z);
            }
            for (int i = 0; i < _lights.Count; i++)
                _lights[i].intensity = _lightIntensity[i] * eased;
        }

        private void SetTrails(bool on)
        {
            foreach (TrailRenderer t in _trails)
                if (t.emitting != on) t.emitting = on;
        }
    }
}
