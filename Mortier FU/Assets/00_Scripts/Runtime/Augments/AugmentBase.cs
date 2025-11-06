using UnityEngine;

namespace MortierFu
{
    public abstract class AugmentBase : IAugment 
    {
        protected SO_Augment augmentData;
        protected PlayerCharacter owner;
        protected SO_CharacterStats stats;

        public PlayerCharacter Owner => owner;
        
        public AugmentBase(SO_Augment augmentData, PlayerCharacter owner)
        {
            this.augmentData = augmentData;
            this.owner = owner;
            this.stats = owner.CharacterStats;
        }
        
        public virtual void Initialize() 
        { }

        public virtual void DeInitialize()
        { }

        public virtual void OnRoundStart(int roundIndex)
        { }

        public virtual void OnShoot(Vector3 targetPos)
        { }

        public virtual void OnImpact(Vector3 impactPos)
        { }

        public virtual void OnKill(PlayerCharacter killedPlayerCharacter)
        { }

        public virtual void OnDeath()
        { }
    }
}