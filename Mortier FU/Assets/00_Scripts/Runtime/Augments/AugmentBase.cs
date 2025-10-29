using UnityEngine;

namespace MortierFu
{
    public abstract class AugmentBase : IAugment 
    {
        protected DA_Augment augmentData;
        protected Character owner;
        protected SO_CharacterStats stats;

        public Character Owner => owner;
        
        public AugmentBase(DA_Augment augmentData, Character owner)
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

        public virtual void OnKill(Character killedCharacter)
        { }

        public virtual void OnDeath()
        { }
    }
}