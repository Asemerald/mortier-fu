using UnityEngine;

namespace MortierFu
{
    public interface IAugment //, IDisposable?
    {
        void Initialize();
        void DeInitialize();
        
        // Hooks
        void OnRoundStart(int roundIndex);
        void OnShoot(Vector3 targetPos);
        void OnImpact(Vector3 impactPos);
        void OnKill(PlayerCharacter killedPlayerCharacter);
        void OnDeath();
    }
}