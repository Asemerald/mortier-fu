using UnityEngine;

namespace MortierFu
{
    public abstract class AugmentBase : IAugment 
    {
        private GameObject _activeVfxPrefab;
        private bool _hasActiveVfx;

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

        protected void ShowVFX()
        {
            if (_hasActiveVfx)
                return;

            if (!augmentData || !augmentData.AugmentVFX || !owner || !owner.AugmentVfxController)
                return;

            _activeVfxPrefab = augmentData.AugmentVFX;
            _hasActiveVfx = true;

            owner.AugmentVfxController.Show(_activeVfxPrefab);
        }

        protected void HideVFX()
        {
            if (!_hasActiveVfx)
                return;

            if (owner && owner.AugmentVfxController && _activeVfxPrefab)
                owner.AugmentVfxController.Hide(_activeVfxPrefab);

            _activeVfxPrefab = null;
            _hasActiveVfx = false;
        }
    }
}