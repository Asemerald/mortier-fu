using System.Collections.Generic;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public sealed class GhostPropPlacementComponent : GhostPawnComponent
    {
        private const int k_overlapCapacity = 64;
        private const float k_aimDeadZoneSqr = 0.04f;

        private readonly Collider[] _overlapResults = new Collider[k_overlapCapacity];
        private readonly List<SO_GhostPlaceableProp> _validProps = new();

        private SO_GhostPlaceableProp _currentProp;

        private GameObject _previewInstance;
        private Renderer[] _previewRenderers;
        private Material[][] _previewOriginalMaterials;
        private Material _lastAppliedPreviewMaterial;

        private Vector2 _aimInput;
        private bool _isAimHeld;
        private bool _isPlacementValid;

        private Vector3 _currentSpawnPosition;
        private Quaternion _currentSpawnRotation = Quaternion.identity;
        private Quaternion _currentAimRotation = Quaternion.identity;

        private InputAction _aimAction;
        private InputAction _toggleAimAction;
        private InputAction _shootAction;

        private float _nextAllowedSpawnTime;
        private bool _didWarnOverlapCapacity;

        public GhostPropPlacementComponent(PlayerGhostPawn pawn) : base(pawn)
        {
        }

        public override void Initialize()
        {
            if (!Settings)
            {
                Logs.LogError("[GhostPropPlacementComponent] Missing GhostSettings.", pawn);
                return;
            }

            if (Owner == null || Owner.PlayerInput == null)
            {
                Logs.LogError("[GhostPropPlacementComponent] Missing PlayerInput.", pawn);
                return;
            }

            _aimAction = Owner.PlayerInput.actions.FindAction("Aim");
            _toggleAimAction = Owner.PlayerInput.actions.FindAction("ToggleAim");
            _shootAction = Owner.PlayerInput.actions.FindAction("Shoot");

            if (_aimAction == null)
                Logs.LogError("[GhostPropPlacementComponent] Aim action not found.", pawn);

            if (_toggleAimAction == null)
                Logs.LogError("[GhostPropPlacementComponent] ToggleAim action not found.", pawn);

            if (_shootAction == null)
                Logs.LogError("[GhostPropPlacementComponent] Shoot action not found.", pawn);

            SelectNextProp();
        }

        public override void OnEnterPawn()
        {
            _aimAction?.Enable();
            _toggleAimAction?.Enable();
            _shootAction?.Enable();

            _aimInput = Vector2.zero;
            _isAimHeld = false;
        }

        public override void OnExitPawn()
        {
            _isAimHeld = false;
            _aimInput = Vector2.zero;

            HidePreview();

            _shootAction?.Disable();
        }

        public void SetAimInput(Vector2 input) => _aimInput = Vector2.ClampMagnitude(input, 1f);

        public void SetAimHeld(bool isHeld)
        {
            _isAimHeld = isHeld;

            if (!_isAimHeld)
                HidePreview();
        }

        public void ShootPressed()
        {
        }

        public void ShootReleased()
        {
        }

        private void TrySpawnCurrentPreview()
        {
            if (!pawn || !pawn.IsPawnActive)
                return;

            if (!_isAimHeld)
                return;

            if (Time.time < _nextAllowedSpawnTime)
                return;

            if (!_currentProp)
                SelectNextProp();

            if (!_currentProp || !EnsurePreview())
                return;

            UpdateAimRotation();
            UpdatePlacementPose();

            _isPlacementValid = ComputePlacementValidity();
            ApplyPreviewState(_isPlacementValid);

            if (!_isPlacementValid)
            {
                Logs.Log("[GhostPropPlacementComponent] Cannot spawn prop: placement is blocked.");
                return;
            }

            SpawnCurrentProp();

            _nextAllowedSpawnTime = Time.time + Mathf.Max(0f, Settings.PropSpawnCooldown);

            SelectNextProp();

            if (_isAimHeld)
                EnsurePreview();
        }

        public override void Tick()
        {
            if (!pawn || !pawn.IsPawnActive)
                return;

            ReadInputState();

            if (!_isAimHeld)
            {
                HidePreview();
                return;
            }

            if (!_currentProp)
                SelectNextProp();

            if (!_currentProp || !EnsurePreview())
                return;

            UpdateAimRotation();
            UpdatePlacementPose();

            _isPlacementValid = ComputePlacementValidity();

            ApplyPreviewState(_isPlacementValid);
            ShowPreview();

            if (_shootAction != null && _shootAction.WasPressedThisFrame())
            {
                TrySpawnCurrentPreview();
            }
        }

        private void ReadInputState()
        {
            _isAimHeld = _toggleAimAction != null && _toggleAimAction.IsPressed();

            _aimInput = _aimAction != null
                ? Vector2.ClampMagnitude(_aimAction.ReadValue<Vector2>(), 1f)
                : Vector2.zero;
        }

        public override void Reset()
        {
            _aimInput = Vector2.zero;
            _isAimHeld = false;
            _isPlacementValid = false;
            _nextAllowedSpawnTime = 0f;

            _currentAimRotation = pawn ? Quaternion.Euler(0f, pawn.transform.eulerAngles.y, 0f) : Quaternion.identity;

            DestroyPreview();
            SelectNextProp();
        }

        public override void Dispose()
        {
            DestroyPreview();

            _aimAction = null;
            _toggleAimAction = null;
            _shootAction = null;
        }

        private void SelectNextProp()
        {
            _validProps.Clear();

            if (!Settings)
                return;

            var props = Settings.PlaceableProps;

            if (props == null || props.Length == 0)
            {
                Logs.LogError("[GhostPropPlacementComponent] No ghost placeable props configured.", pawn);
                _currentProp = null;
                return;
            }

            for (int i = 0; i < props.Length; i++)
            {
                if (props[i] && props[i].RealPrefab)
                    _validProps.Add(props[i]);
            }

            if (_validProps.Count == 0)
            {
                Logs.LogError("[GhostPropPlacementComponent] No valid ghost placeable props found.", pawn);
                _currentProp = null;
                return;
            }

            SO_GhostPlaceableProp previousProp = _currentProp;

            if (_validProps.Count == 1)
            {
                _currentProp = _validProps[0];
            }
            else
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    SO_GhostPlaceableProp candidate = _validProps[Random.Range(0, _validProps.Count)];

                    if (candidate == previousProp) continue;

                    _currentProp = candidate;
                    break;
                }

                if (!_currentProp || _currentProp == previousProp)
                    _currentProp = _validProps[Random.Range(0, _validProps.Count)];
            }

            DestroyPreview();
        }

        private bool EnsurePreview()
        {
            if (_previewInstance)
                return true;

            if (!_currentProp)
                return false;

            GameObject previewPrefab = _currentProp.PreviewPrefab;

            if (!previewPrefab)
            {
                Logs.LogError("[GhostPropPlacementComponent] Preview prefab is missing.", pawn);
                return false;
            }

            _previewInstance = Object.Instantiate(previewPrefab);
            _previewInstance.name = $"Ghost Preview - {previewPrefab.name}";

            DisablePreviewPhysics(_previewInstance);
            CachePreviewRenderers();

            _previewInstance.SetActive(false);

            return true;
        }

        private void DisablePreviewPhysics(GameObject preview)
        {
            if (!preview)
                return;

            var colliders = preview.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i])
                    colliders[i].enabled = false;
            }

            var rigidbodies = preview.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Rigidbody rb = rigidbodies[i];

                if (!rb)
                    continue;

                rb.useGravity = false;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void CachePreviewRenderers()
        {
            if (!_previewInstance)
                return;

            _previewRenderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
            _previewOriginalMaterials = new Material[_previewRenderers.Length][];

            for (int i = 0; i < _previewRenderers.Length; i++)
            {
                Renderer renderer = _previewRenderers[i];

                if (!renderer)
                    continue;

                _previewOriginalMaterials[i] = renderer.sharedMaterials;
            }

            _lastAppliedPreviewMaterial = null;
        }

        private void UpdateAimRotation()
        {
            if (_aimInput.sqrMagnitude < k_aimDeadZoneSqr)
                return;

            Vector3 forward = new(_aimInput.x, 0f, _aimInput.y);

            if (forward.sqrMagnitude < 0.0001f)
                return;

            _currentAimRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void UpdatePlacementPose()
        {
            if (!_currentProp)
                return;

            Vector3 basePosition = ResolveGroundedPlacementPosition();

            _currentSpawnRotation = _currentAimRotation * _currentProp.RotationOffset;
            _currentSpawnPosition = basePosition + _currentSpawnRotation * _currentProp.SpawnOffset;

            if (_previewInstance)
            {
                _previewInstance.transform.SetPositionAndRotation(
                    _currentSpawnPosition,
                    _currentSpawnRotation
                );
            }
        }

        private Vector3 ResolveGroundedPlacementPosition()
        {
            if (!pawn || !Settings)
                return Vector3.zero;

            Vector3 origin = pawn.transform.position;
            Vector3 rayStart = origin + Vector3.up * Settings.GroundRaycastStartHeight;

            return Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                Settings.GroundRaycastLength,
                Settings.GroundMask,
                QueryTriggerInteraction.Ignore)
                ? hit.point
                : origin;
        }

        private bool ComputePlacementValidity()
        {
            if (!_currentProp || !Settings)
                return false;

            Vector3 center = _currentSpawnPosition +
                             _currentSpawnRotation * _currentProp.ValidationBoxCenter;

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                _currentProp.ValidationBoxHalfExtents,
                _overlapResults,
                _currentSpawnRotation,
                Settings.PlacementBlockingMask,
                QueryTriggerInteraction.Collide
            );
            
            
            if (IsWaterOnTop())
            {
                return false;
            }
                

            if (hitCount >= _overlapResults.Length)
            {
                if (_didWarnOverlapCapacity) return false;

                Logs.LogWarning(
                    "[GhostPropPlacementComponent] Placement overlap buffer is full. Placement is considered invalid.",
                    pawn);
                _didWarnOverlapCapacity = true;

                return false;
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapResults[i];

                if (ShouldIgnoreCollider(hit))
                    continue;

                return false;
            }

            return true;
        }

        private bool IsWaterOnTop()
        {
            RaycastHit hit;
            
            if (Physics.Raycast(
                    _currentSpawnPosition + Vector3.up * Settings.WaterRaycastHeightMargin,
                    Vector3.down, out hit, Settings.WaterRaycastLength))
            {
                if ((Settings.WaterLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    return true;
                }
                    
                return false;
            }
            
            return true;
        }

        private bool ShouldIgnoreCollider(Collider hit)
        {
            if (!hit)
                return true;

            Transform hitTransform = hit.transform;

            if (_previewInstance && hitTransform.IsChildOf(_previewInstance.transform))
                return true;

            if (pawn && hitTransform.IsChildOf(pawn.transform))
                return true;

            PlayerCharacter player = hit.GetComponentInParent<PlayerCharacter>();

            if (!player) return false;
            if (pawn && player == pawn.SourceCharacter)
                return true;

            return player.Health == null || !player.Health.IsAlive;
        }

        private void ApplyPreviewState(bool isValid)
        {
            Material targetMaterial = isValid
                ? Settings.ValidPreviewMaterial
                : Settings.InvalidPreviewMaterial;

            if (targetMaterial)
            {
                ApplyPreviewMaterial(targetMaterial);
                return;
            }

            RestorePreviewMaterials();
        }

        private void ApplyPreviewMaterial(Material material)
        {
            if (!material || _previewRenderers == null)
                return;

            if (_lastAppliedPreviewMaterial == material)
                return;

            for (int i = 0; i < _previewRenderers.Length; i++)
            {
                Renderer renderer = _previewRenderers[i];

                if (!renderer)
                    continue;

                var materials = renderer.sharedMaterials;

                for (int m = 0; m < materials.Length; m++)
                {
                    materials[m] = material;
                }

                renderer.sharedMaterials = materials;
            }

            _lastAppliedPreviewMaterial = material;
        }

        private void RestorePreviewMaterials()
        {
            if (_previewRenderers == null || _previewOriginalMaterials == null)
                return;

            if (_lastAppliedPreviewMaterial == null)
                return;

            for (int i = 0; i < _previewRenderers.Length; i++)
            {
                Renderer renderer = _previewRenderers[i];

                if (!renderer)
                    continue;

                if (_previewOriginalMaterials[i] == null)
                    continue;

                renderer.sharedMaterials = _previewOriginalMaterials[i];
            }

            _lastAppliedPreviewMaterial = null;
        }

        private void SpawnCurrentProp()
        {
            if (!_currentProp || !_currentProp.RealPrefab)
                return;

            GameObject spawnedProp =
                Object.Instantiate(_currentProp.RealPrefab, _currentSpawnPosition, _currentSpawnRotation);

            GhostSystem ghostSystem = SystemManager.Instance?.Get<GhostSystem>();

            if (ghostSystem == null)
            {
                Logs.LogWarning(
                    "[GhostPropPlacementComponent] Spawned prop but GhostSystem was not found. Prop will not be auto-cleaned.",
                    pawn);
                return;
            }

            ghostSystem.RegisterSpawnedProp(Owner, spawnedProp);

            Logs.Log($"[GhostPropPlacementComponent] Spawned ghost prop '{_currentProp.RealPrefab.name}'.");
        }

        private void ShowPreview()
        {
            if (_previewInstance && !_previewInstance.activeSelf)
                _previewInstance.SetActive(true);
        }

        private void HidePreview()
        {
            if (_previewInstance && _previewInstance.activeSelf)
                _previewInstance.SetActive(false);
        }

        private void DestroyPreview()
        {
            if (_previewInstance)
                Object.Destroy(_previewInstance);

            _previewInstance = null;
            _previewRenderers = null;
            _previewOriginalMaterials = null;
            _lastAppliedPreviewMaterial = null;
        }
    }
}