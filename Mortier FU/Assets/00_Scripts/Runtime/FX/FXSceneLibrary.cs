using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class FXSceneLibrary : MonoBehaviour
    {
        [Header("Bombshell")]
        [SerializeField] private GameObject _bombshellPreview;
        [SerializeField] private ParticleSystem[] _bombshellExplosionColors;
        [SerializeField] private ParticleSystem _bombshellWaterExplosion;

        [Header("Player")]
        [SerializeField] private ParticleSystem _dash;
        [SerializeField] private ParticleSystem _stun;

        public GameObject BombshellPreview => _bombshellPreview;
        public ParticleSystem[] BombshellExplosionColors => _bombshellExplosionColors;
        public ParticleSystem BombshellWaterExplosion => _bombshellWaterExplosion;
        public ParticleSystem Dash => _dash;
        public ParticleSystem Stun => _stun;

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