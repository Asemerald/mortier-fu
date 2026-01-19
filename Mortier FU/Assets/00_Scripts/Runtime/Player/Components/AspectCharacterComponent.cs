using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    [System.Serializable]
    public struct CharacterAspectMaterials
    {
        public Material BurnBaseVoronoiMat;
        public Material DotsAlphaSpikesMat;
        public Material OrangeSpikesMat;
        public Material TrailThinMat;
        public Material TrailFatMat;
        public ParticleSystem.MinMaxGradient LightColor;
        [ColorUsage(true)] public Color PlayerColor;
        public Material PlayerMaterial;
        public Material PlayerOutlineMaterial;
        public SkinnedMeshRenderer[] PlayerMeshes;
        public SkinnedMeshRenderer[] PlayerOutlineMeshes;
        public GameObject SpawnVFXPrefab;
        public Material DashTrailMaterial;
        public GameObject TombPrefab;
    }

    public class AspectCharacterComponent : CharacterComponent
    {
        private Sequence _blinkTween;

        private Material _materialInstance;
        private Material _outlineMaterialInstance;

        private ParticleSystem _particleSystemInstance;

        private GameObject _spawnVFXInstance;
        private Color _startingColor;

        private Color _startingOutlineColor;

        public AspectCharacterComponent(PlayerCharacter character) : base(character)
        {
        }

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
            var renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length <= 0)
                return;

            _materialInstance = new Material(AspectMaterials.PlayerMaterial);
            _outlineMaterialInstance = new Material(AspectMaterials.PlayerOutlineMaterial);

            foreach (var mesh in AspectMaterials.PlayerMeshes)
            {
                mesh.material = _materialInstance;
            }

            foreach (var mesh in AspectMaterials.PlayerOutlineMeshes)
            {
                mesh.material = _outlineMaterialInstance;
            }

            _startingOutlineColor = _outlineMaterialInstance.color;
            _startingColor = _materialInstance.color;

            if (SpawnVFXPrefab)
            {
                _spawnVFXInstance = Object.Instantiate(SpawnVFXPrefab, character.transform.position,
                    SpawnVFXPrefab.transform.rotation);
                _spawnVFXInstance.SetActive(false);

                _particleSystemInstance = _spawnVFXInstance.GetComponent<ParticleSystem>();
            }
        }

        public void PlayDamageBlink(Color blinkColor, int blinkCount = 5, float blinkDuration = 0.08f)
        {
            if (_materialInstance == null || _outlineMaterialInstance == null)
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
                    _materialInstance.color = _startingColor;
                    _outlineMaterialInstance.color = _startingOutlineColor;
                });
        }

        public async UniTask PlayVFXSequential(PlayerCharacter[] characters,
            Action<PlayerCharacter> onVFXCompleted = null)
        {
            foreach (var character in characters)
            {
                if (_spawnVFXInstance == null)
                {
                    onVFXCompleted?.Invoke(character);
                    continue;
                }

                _spawnVFXInstance.transform.position = character.transform.position;
                _spawnVFXInstance.SetActive(true);

                character.ShakeService.ShakeController(character.Owner, ShakeService.ShakeType.MID);

                _particleSystemInstance?.Play();

                float totalDuration = 2f;
                float onCompleteDelay = 0.5f;

                _ = UniTask.Delay(TimeSpan.FromSeconds(totalDuration)).ContinueWith(() =>
                {
                    _spawnVFXInstance.SetActive(false);
                    _particleSystemInstance.Stop();
                });

                await UniTask.Delay(TimeSpan.FromSeconds(onCompleteDelay));
                onVFXCompleted?.Invoke(character);
            }
        }

        public override void Dispose()
        {
            _blinkTween.Stop();
        }
    }
}