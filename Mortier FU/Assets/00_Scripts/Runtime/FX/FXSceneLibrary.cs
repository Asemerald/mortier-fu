using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class FXSceneLibrary : MonoBehaviour
    {
        [Header("Bombshell")]
        [SerializeField] private ParticleSystem _bombshellPreview;
        [SerializeField] private ParticleSystem[] _bombshellExplosionColors;
        [SerializeField] private ParticleSystem _bombshellWaterExplosion;

        [Header("Player")]
        [SerializeField] private ParticleSystem _dash;

        public ParticleSystem BombshellPreview => _bombshellPreview;
        public ParticleSystem[] BombshellExplosionColors => _bombshellExplosionColors;
        public ParticleSystem BombshellWaterExplosion => _bombshellWaterExplosion;
        public ParticleSystem Dash => _dash;

        private void Awake()
        {
            var fxService = ServiceManager.Instance?.Get<FXService>();

            if (fxService == null)
            {
                Logs.LogError("[FXSceneLibrary] FXService is not available.");
                return;
            }

            fxService.RegisterLibrary(this);
        }

        private void OnDestroy()
        {
            var fxService = ServiceManager.Instance?.Get<FXService>();

            if (fxService == null)
                return;

            fxService.UnregisterLibrary(this);
        }
    }
}