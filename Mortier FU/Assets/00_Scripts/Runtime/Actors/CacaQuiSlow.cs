using UnityEngine;

namespace MortierFu
{
    public class CacaQuiSlow : BaseZone
    {
        #region Variables
        
        [SerializeField] private float slowMultiplier = 0.5f;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private GameObject vfxCacaQuiSlowPrefab;
        
        #endregion

        #region Unity Lifecycle

        protected void OnTriggerEnter(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            
            if (!player || !_counters.TryAdd(player, vfxFootPrintDuration)) return;

            ApplyEffectZoneEnter(player);
        }

        protected void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            
            if (!player || !_counters.Remove(player)) return;

            ApplyEffectZoneExit(player);
        }

        
        private void Update() => ApplyEffectZoneTick();
        

        #endregion

        #region Base Logic

        protected override void ApplyEffectZoneEnter(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier(slowMultiplier, transitionDuration);
        }

        protected override void ApplyEffectZoneExit(PlayerCharacter player)
        {
            player.SetExternalSpeedMultiplier(1f, transitionDuration);
        }

        protected override void ApplyEffectZoneTick()
        {
            int counter = _counters.Count;
            
            if (counter == 0) return;
            
            _playersCache.Clear();
            _playersCache.AddRange(_counters.Keys);
            
            foreach (var player in  _playersCache)
            {
                if (_counters[player] <= 0f)
                {
                    PlayCacaQuiSlowVFX(player);
                    _counters[player] = vfxFootPrintDuration;
                }
                else
                {
                    _counters[player] -= Time.deltaTime;
                }
            }
        }

        #endregion

        private void PlayCacaQuiSlowVFX(PlayerCharacter player)
        {
            if (player.ExternalSpeedMultiplier > 0.5f) return;
            
            var caca = Instantiate(vfxCacaQuiSlowPrefab, player.FeetPoint.position,
                player.FeetPoint.rotation);
            
            Destroy(caca, 10f);
        }
        
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            
            if (col) col.isTrigger = true;
            
            _counters.Clear();
            _playersCache.Clear();
        }

        
    }
}