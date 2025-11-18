using System;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public class ACT_Storm : MonoBehaviour
    {
        public Vector3 Eye
        {
            get => transform.position;
            set => transform.position = value;
        }

        private float _currentRadius;
        private SO_StormSettings _settings;
        private FrequencyTimer _damageTimer;
        
        // Dependencies
        private GameModeBase _gm;
        
        public void Initialize(Vector3 eye, SO_StormSettings settings)
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            _settings = settings;
            
            Eye = eye;
            _currentRadius = _settings.MaxRadius;
            
            float duration = (_settings.MaxRadius - _settings.MinRadius) / _settings.ShrinkSpeed;
            Tween.Custom(_settings.MaxRadius, _settings.MinRadius, duration, OnRadiusShrink);
            
            _damageTimer = new FrequencyTimer(settings.TicksPerSecond);
            _damageTimer.OnTick += DamagePlayers;
            _damageTimer.Start();
        }
        
        private void OnRadiusShrink(float radius)
        {
            _currentRadius = radius;
        }

        public void Stop()
        {
            _damageTimer.Stop();
            Destroy(gameObject);
        }

        private void DamagePlayers()
        {
            var alivePlayers = _gm.AlivePlayers;

            for (int i = alivePlayers.Count - 1; i >= 0; i--)
            {
                PlayerCharacter player = alivePlayers[i];
                if (IsInBounds(player)) continue;
                player.Health.TakeDamage(_settings.DamageAmount, this);
            }
        }

        private bool IsInBounds(PlayerCharacter character)
        {
            float sqrDist = (Eye - character.transform.position).sqrMagnitude;
            return sqrDist < _currentRadius * _currentRadius;
        }
    
        void OnDestroy()
        {
            _damageTimer.Dispose();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.mediumPurple;
            Gizmos.DrawWireSphere(Eye, _currentRadius);
        }
    }
}