using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public abstract class AugmentBase : IAugment 
    {
        protected SO_Augment augmentData;
        protected PlayerCharacter owner;
        protected SO_CharacterStats stats;
        protected SO_AugmentDatabase db;

        public PlayerCharacter Owner => owner;
        
        public AugmentBase(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db)
        {
            this.augmentData = augmentData;
            this.owner = owner;
            this.stats = owner.Stats;
            this.db = db;
        }

        private GameObject ps;
        
        public virtual void Initialize() 
        { }

        public virtual void Reset()
        { }

        public virtual void Dispose()
        { }

        public void InstantiateVFX()
        {
            if (augmentData == null)
                return;
            
            if (augmentData.AugmentVFX == null)
                return;
            
            ClearVFXInstance();
            
            //Logs.LogError($"InstantiateVFX");
            ps = Object.Instantiate(augmentData.AugmentVFX, owner.transform);
        }

        public void ClearVFXInstance()
        {
            if (ps == null)
                return;
            
            Object.Destroy(ps);
        }
    }
}