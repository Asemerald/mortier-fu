using System;
using UnityEngine;

namespace MortierFu
{
    public class AugmentFXPickUp : MonoBehaviour
    {
        private GameModeBase _gm;
        [SerializeField]
        private ParticleSystem _particleSystem;

        
        private void Awake()
        {
            if (_particleSystem == null)
                _particleSystem = GetComponent<ParticleSystem>();
        }
        private void OnEnable()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
        }

        public void Init() => _gm.OnRoundStarted += OnChangeScene;
        
        private void OnChangeScene(RoundInfo info)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(gameObject);
        }
        
        private void OnDisable()
        {
            if (_gm != null)
                _gm.OnRoundStarted -= OnChangeScene;
        }
    }
}
