using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    [Serializable]
    public struct CharacterAspectMaterials
    {
        public Material WhiteSpike00;
        public Material WhiteSpike01;
        public Material BurnBaseVoronoiMat;
        public Material DotsAlphaSpikesMat;
        public Material OrangeSpikesMat;
        public Material TrailThinMat;
        public Material TrailFatMat;
        public ParticleSystem.MinMaxGradient LightColor;
        [ColorUsage(true)] public Color PlayerColor;
        public Material PlayerMaterial;
        public Material PlayerOutlineMaterial;
        public SkinnedMeshRenderer PlayerMesh;
        public SkinnedMeshRenderer PlayerCrownMesh;
        public SkinnedMeshRenderer PlayerCustomMesh;
        public SkinnedMeshRenderer PlayerTailMesh;
        public GameObject SpawnVFXPrefab;
        public Material DashTrailMaterial;
    }

    public class AspectCharacterComponent : CharacterComponent
    {
        private const int k_materialFirstSlot = 0;
        private const int k_materialSecondSlot = 1;

        private Sequence _blinkTween;
        private Sequence _reloadWidgetTween;

        private Material _materialInstance;
        private Material _outlineMaterialInstance;

        private ParticleSystem _particleSystemInstance;
        private GameObject _spawnVFXInstance;

        private Color _startingColor;
        private Color _startingOutlineColor;

        private ShakeService _shakeService;

        public AspectCharacterComponent(PlayerCharacter character) : base(character)
        { }

        public Color PlayerColor => AspectMaterials.PlayerColor;
        private GameObject SpawnVFXPrefab => AspectMaterials.SpawnVFXPrefab;
        public Material GetDashTrailMaterial() => AspectMaterials.DashTrailMaterial;

        public CharacterAspectMaterials AspectMaterials { get; private set; }

        public void SetAspectMaterials(CharacterAspectMaterials aspectMaterials)
        {
            AspectMaterials = aspectMaterials;
        }

        public override void Initialize()
        {
            if (!ValidateMaterials())
                return;

            _materialInstance = new Material(AspectMaterials.PlayerMaterial);
            _outlineMaterialInstance = new Material(AspectMaterials.PlayerOutlineMaterial);

            ApplyRuntimeMaterials();

            _startingColor = _materialInstance.color;
            _startingOutlineColor = _outlineMaterialInstance.color;

            InitializeSpawnVfx();

            _shakeService = ServiceManager.Instance.Get<ShakeService>();

            if (character.ControlContext == PlayerControlContext.LobbySandbox)
                Character.Aspect.PlayVFXSequential(new[] { Character }).Forget();
        }

        private bool ValidateMaterials()
        {
            if (!AspectMaterials.PlayerMaterial)
            {
                Logs.LogError("[AspectCharacterComponent] PlayerMaterial is missing.", character);
                return false;
            }

            if (!AspectMaterials.PlayerOutlineMaterial)
            {
                Logs.LogError("[AspectCharacterComponent] PlayerOutlineMaterial is missing.", character);
                return false;
            }

            if (!AspectMaterials.PlayerMesh)
            {
                Logs.LogError("[AspectCharacterComponent] PlayerMesh is missing.", character);
                return false;
            }

            return true;
        }

        private void ApplyRuntimeMaterials()
        {
            SetMaterialSlot(AspectMaterials.PlayerMesh, k_materialFirstSlot, _outlineMaterialInstance);
            
            SetMaterialSlot(AspectMaterials.PlayerMesh, k_materialSecondSlot, _materialInstance);

            SetMaterialSlot(AspectMaterials.PlayerCrownMesh, k_materialSecondSlot, _outlineMaterialInstance);

            SetMaterialSlot(AspectMaterials.PlayerCustomMesh, k_materialSecondSlot, _outlineMaterialInstance);

            SetMaterialSlot(AspectMaterials.PlayerTailMesh, k_materialFirstSlot, _materialInstance);
            SetMaterialSlot(AspectMaterials.PlayerTailMesh, k_materialSecondSlot, _outlineMaterialInstance);
        }

        private static void SetMaterialSlot(Renderer renderer, int slotIndex, Material material)
        {
            if (!renderer || !material)
                return;

            var materials = renderer.sharedMaterials;

            if (slotIndex < 0 || slotIndex >= materials.Length)
            {
                Logs.LogWarning(
                    $"[AspectCharacterComponent] Renderer '{renderer.name}' has no material slot {slotIndex}.",
                    renderer
                );

                return;
            }

            materials[slotIndex] = material;
            renderer.sharedMaterials = materials;
        }

        private void InitializeSpawnVfx()
        {
            if (!SpawnVFXPrefab)
                return;

            _spawnVFXInstance = Object.Instantiate(SpawnVFXPrefab, character.transform.position, SpawnVFXPrefab.transform.rotation);

            _spawnVFXInstance.SetActive(false);
            _particleSystemInstance = _spawnVFXInstance.GetComponent<ParticleSystem>();
        }

        public void PlayDamageBlink(Color blinkColor, int blinkCount = 5, float blinkDuration = 0.08f)
        {
            if (!_materialInstance || !_outlineMaterialInstance)
                return;

            if (_blinkTween.isAlive)
                _blinkTween.Stop();

            if (_materialInstance.color == blinkColor && _outlineMaterialInstance.color == blinkColor)
                return;

            _materialInstance.color = _startingColor;
            _outlineMaterialInstance.color = _startingOutlineColor;

            _blinkTween = Sequence.Create()
                .Group(
                    Tween.MaterialColor(
                        _materialInstance,
                        blinkColor,
                        blinkDuration,
                        ease: Ease.InBack,
                        cycles: blinkCount * 2,
                        cycleMode: CycleMode.Yoyo
                    )
                )
                .Group(
                    Tween.MaterialColor(
                        _outlineMaterialInstance,
                        blinkColor,
                        blinkDuration,
                        ease: Ease.InBack,
                        cycles: blinkCount * 2,
                        cycleMode: CycleMode.Yoyo
                    )
                )
                .OnComplete(() =>
                {
                    if (_materialInstance)
                        _materialInstance.color = _startingColor;

                    if (_outlineMaterialInstance)
                        _outlineMaterialInstance.color = _startingOutlineColor;
                });
        }

        public async UniTask ReloadCompleteFeedback()
        {
            if (_reloadWidgetTween.isAlive)
                _reloadWidgetTween.Stop();

            var startColor = Character.Mortar.AimWidget.MaterialInstance.color;
            var finalColor = startColor + new Color(0.25f, 0.25f, 0.25f);

            _reloadWidgetTween = Sequence.Create()
                .Group(
                    Tween.MaterialColor(
                        Character.Mortar.AimWidget.MaterialInstance,
                        finalColor,
                        0.08f,
                        ease: Ease.InOutSine,
                        cycles: 2,
                        cycleMode: CycleMode.Yoyo
                    )
                )
                .OnComplete(() =>
                {
                    Character.Mortar.AimWidget.MaterialInstance.color = startColor;
                });

            await UniTask.CompletedTask;
        }

        public async UniTask PlayVFXSequential(PlayerCharacter[] characters, Action<PlayerCharacter> onVFXCompleted = null)
        {
            foreach (var playerCharacter in characters)
            {
                if (!_spawnVFXInstance)
                {
                    onVFXCompleted?.Invoke(playerCharacter);
                    continue;
                }

                _spawnVFXInstance.transform.position = playerCharacter.transform.position;
                _spawnVFXInstance.SetActive(true);

                AudioService.PlayOneShot(
                    AudioService.FMODEvents.SFX_Player_Summon,
                    playerCharacter.transform.position
                );

                _shakeService?.ShakeController(playerCharacter.Owner, ShakeService.ShakeType.MID);

                _particleSystemInstance?.Play();

                var totalDuration = 2f;
                var onCompleteDelay = 0.5f;

                _ = UniTask.Delay(TimeSpan.FromSeconds(totalDuration)).ContinueWith(() =>
                {
                    if (_spawnVFXInstance)
                        _spawnVFXInstance.SetActive(false);

                    _particleSystemInstance?.Stop();
                });

                await UniTask.Delay(TimeSpan.FromSeconds(onCompleteDelay));

                onVFXCompleted?.Invoke(playerCharacter);
            }
        }

        public override void Dispose()
        {
            if (_blinkTween.isAlive)
                _blinkTween.Stop();

            if (_reloadWidgetTween.isAlive)
                _reloadWidgetTween.Stop();

            if (_materialInstance)
            {
                Object.Destroy(_materialInstance);
                _materialInstance = null;
            }

            if (_outlineMaterialInstance)
            {
                Object.Destroy(_outlineMaterialInstance);
                _outlineMaterialInstance = null;
            }

            if (!_spawnVFXInstance) return;
            
            Object.Destroy(_spawnVFXInstance);
            _spawnVFXInstance = null;
            _particleSystemInstance = null;
        }
    }
}