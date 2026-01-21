namespace MortierFu
{
    public abstract class AugmentHealthThresholdBase : AugmentBase
    {
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        
        protected abstract float HealthThreshold { get; }
        protected bool IsActive { get; private set; }

        protected AugmentHealthThresholdBase(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }
        
        public override void Initialize()
        {
            _healthChangedBinding = new EventBinding<TriggerHealthChanged>(OnHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_healthChangedBinding);
            
            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);
        }
        
        protected abstract void OnEnterThreshold();
        protected abstract void OnExitThreshold();
        
        private void OnHealthChanged(TriggerHealthChanged evt)
        {
            if (evt.Character != owner) return;

            float currentRatio = evt.NewHealth / evt.MaxHealth;
            bool active = currentRatio <= HealthThreshold;
            if (active && !IsActive)
            {
                IsActive = true;
                OnEnterThreshold();
            }
            else if (!active && IsActive)
            {
                IsActive = false;
                OnExitThreshold();
            }
        }
        
        private void OnEndRound(TriggerEndRound evt)
        {
            IsActive = false;
            OnExitThreshold();
        }

        public override void Dispose()
        {
            EventBus<TriggerHealthChanged>.Deregister(_healthChangedBinding);
        }
    }
}