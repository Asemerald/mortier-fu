using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class AugmentPickup : MonoBehaviour
    {
        [SerializeField] private E_AugmentRarity _rarity;

        [SerializeField] private AugmentPickupVisual[] _augmentVFXRarityPrototypes;
        [SerializeField] private GameObject[] _pickupVFX;

        private Transform _attachmentPoint;
        private Vector3 _attachmentLocalOffset;

        private GameObject _vfxInstance;
        private AugmentPickupVisual _visualInstance;

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private ParticleSystem[] _particleSystems;

        private bool _isInteractable;

        private int _index;

        private AugmentSelectionSystem _system;
        private ShakeService _shakeService;

        private Quaternion _initialRotation;

        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;
            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            _initialRotation = transform.rotation;
            CacheRuntimeComponents(force: true);
            SetInteractable(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isInteractable)
                return;

            if (other.attachedRigidbody == null)
                return;

            if (!other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
                return;

            bool success = _system.NotifyPlayerInteraction(character, _index);

            if (!success)
                return;

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Grab, transform.position);
            _shakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);

            Instantiate(_pickupVFX[(int)_rarity], transform.position, transform.rotation);

            Reset();
        }

        public void Reset()
        {
            AttachToPoint(null);
            SetRenderersEnabled(true);
            SetInteractable(false);

            transform.rotation = _initialRotation;

            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = 0f;
            transform.eulerAngles = eulerAngles;

            gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible && !gameObject.activeSelf)
                gameObject.SetActive(true);

            CacheRuntimeComponents();

            SetRenderersEnabled(visible);

            if (visible)
            {
                SetVfx();
                return;
            }

            HideVfx();
        }

        private void PlayParticles()
        {
            CacheRuntimeComponents();

            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i])
                    _particleSystems[i].Play(true);
            }
        }

        private void StopParticles()
        {
            CacheRuntimeComponents();

            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i])
                    _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            CacheRuntimeComponents();

            if (_colliders == null)
                return;

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i])
                    _colliders[i].enabled = interactable;
            }
        }

        public void MoveTo(Vector3 position)
        {
            AttachToPoint(null);
            transform.position = position;
        }

        public void AttachToPoint(Transform point) => AttachTo(point, Vector3.zero);

        public void AttachTo(Transform point, Vector3 localOffset)
        {
            _attachmentPoint = point;
            _attachmentLocalOffset = localOffset;

            if (point)
                transform.position = point.TransformPoint(localOffset);
        }

        public async UniTask DropToAsync(Vector3 targetPosition, float jumpHeight, float duration, CancellationToken cancellationToken)
        {
            duration = Mathf.Max(0.05f, duration);
            jumpHeight = Mathf.Max(0f, jumpHeight);

            AttachToPoint(null);
            SetInteractable(false);
            SetVisible(true);
            SetVfx();
            
            Vector3 startPosition = transform.position;
            Vector3 midPosition = Vector3.Lerp(startPosition, targetPosition, 0.5f) + Vector3.up * jumpHeight;
            float halfDuration = duration * 0.5f;

            await Tween.Position(transform, midPosition, halfDuration, Ease.OutQuad).ToUniTask(cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await Tween.Position(transform, targetPosition, halfDuration, Ease.InQuad).ToUniTask(cancellationToken: cancellationToken);

            SetInteractable(true);
        }

        public void SetAugmentVisual(SO_Augment augment)
        {
            if (!augment)
                return;

            AugmentPickupVisual prototype = GetVFXRarityPrototype(augment.Rarity);

            if (_vfxInstance)
            {
                Destroy(_vfxInstance);
                _vfxInstance = null;
                _visualInstance = null;
            }

            _vfxInstance = Instantiate(prototype.gameObject, transform.position, transform.rotation, transform);

            _vfxInstance.transform.localPosition = Vector3.zero;
            _vfxInstance.transform.localRotation = Quaternion.identity;
            _vfxInstance.transform.localScale = Vector3.one;

            _visualInstance = _vfxInstance.GetComponent<AugmentPickupVisual>();

            if (_visualInstance)
                _visualInstance.SetLogoSprite(augment.SmallSprite);

            _rarity = prototype.Rarity;

            CacheRuntimeComponents(force: true);
            SetRenderersEnabled(true);
            SetInteractable(false);
        }

        public void HideVfx()
        {
            if (_visualInstance)
                _visualInstance.HideVfx();

            StopParticles();
        }

        public void SetVfx()
        {
            if (_visualInstance)
                _visualInstance.SetVfx();

            PlayParticles();
        }
        
        private void Update()
        {
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = 0f;
            transform.eulerAngles = eulerAngles;

            if (!_attachmentPoint)
                return;

            transform.position = _attachmentPoint.TransformPoint(_attachmentLocalOffset);
        }

        private AugmentPickupVisual GetVFXRarityPrototype(E_AugmentRarity rarity)
        {
            foreach (AugmentPickupVisual prototype in _augmentVFXRarityPrototypes)
            {
                if (rarity != prototype.Rarity)
                    continue;

                return prototype;
            }

            throw new Exception($"Prototype not found for rarity {rarity}");
        }

        private void SetRenderersEnabled(bool enabled)
        {
            CacheRuntimeComponents();

            if (_renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i])
                    _renderers[i].enabled = enabled;
            }
        }

        private void CacheRuntimeComponents(bool force = false)
        {
            if (!force && _renderers != null && _colliders != null && _particleSystems != null)
                return;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }
    }
}
