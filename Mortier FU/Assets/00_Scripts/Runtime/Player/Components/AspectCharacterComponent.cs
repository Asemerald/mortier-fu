using PrimeTween;
using UnityEngine;

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
        public SkinnedMeshRenderer[] PlayerMeshes;
    }

    public class AspectCharacterComponent : CharacterComponent
    {
        public Color PlayerColor => AspectMaterials.PlayerColor;

        public CharacterAspectMaterials AspectMaterials { get; private set; }

        private Material _materialInstance;
        private Tween _blinkTween;

        public AspectCharacterComponent(PlayerCharacter character) : base(character)
        {
        }

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

            foreach (var mesh in AspectMaterials.PlayerMeshes)
            {
                mesh.material = _materialInstance;
            }
        }

        public void PlayDamageBlink(
            Color blinkColor,
            int blinkCount = 5,
            float blinkDuration = 0.15f
        )
        {
            if (_materialInstance == null)
                return;

            if (_blinkTween.isAlive)
                _blinkTween.Stop();

            _materialInstance.color = PlayerColor;

            _blinkTween = Tween.MaterialColor(
                target: _materialInstance,
                endValue: blinkColor,
                duration: blinkDuration,
                ease: Ease.InBack,
                cycles: (blinkCount * 2),
                cycleMode: CycleMode.Yoyo
            ).OnComplete(() => { _materialInstance.color = PlayerColor; });
        }

        public override void Dispose()
        {
            _blinkTween.Stop();
        }
        // private static Color GetColorForPlayerIndex(int index, int totalPlayers,
        //     float hueOffset = 0f, float saturation = 1f, float value = 1f)
        // {
        //     float segment = 1f / Mathf.Max(1, totalPlayers);
        //     float hue = (segment * index + hueOffset) % 1f;
        //     return Color.HSVToRGB(hue, saturation, value);
        // }
    }
}