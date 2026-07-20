using UnityEngine;

namespace MortierFu
{
    public class AugmentFXPickUp : MonoBehaviour
    {
        private GameModeBase _gm;

        [SerializeField] private ParticleSystem _particleSystem;

        private void Awake()
        {
            if (!_particleSystem)
                _particleSystem = GetComponent<ParticleSystem>();
        }

        private void OnEnable() => _gm = GameService.CurrentGameMode as GameModeBase;

        public void Init()
        {
            if (_gm == null)
                _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
                return;

            _gm.OnRoundStarted -= OnChangeScene;
            _gm.OnRoundStarted += OnChangeScene;
        }

        private void OnChangeScene(RoundInfo info)
        {
            if (_particleSystem)
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Destroy(gameObject);
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_gm == null)
                return;

            _gm.OnRoundStarted -= OnChangeScene;
            _gm = null;
        }
    }
}