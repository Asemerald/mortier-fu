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

        private SO_StormSettings _settings;
        private FrequencyTimer _damageTimer;
        
        // Dependencies
        private GameModeBase _gm;
        
        public void Initialize(Vector3 eye, SO_StormSettings settings)
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            _settings = settings;
            
            Eye = eye;

            _damageTimer = new FrequencyTimer(settings.TicksPerSecond);
            _damageTimer.OnTick += DamagePlayers;
            _damageTimer.Start();
        }

        public void Stop()
        {
            _damageTimer.Stop();
            Destroy(gameObject);
        }

        private void DamagePlayers()
        {
            var alivePlayers = _gm.AlivePlayers;

            foreach (var player in alivePlayers)
            {
                player.Health.TakeDamage(_settings.DamageAmount, this);
            }
        }
    
        void OnDestroy()
        {
            _damageTimer.Dispose();
        }
    }
}