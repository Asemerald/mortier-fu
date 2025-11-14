namespace MortierFu
{
    public abstract class AugmentHealthThresholdBase : AugmentBase
    {
        private EventBinding<TriggerHealthChanged> _healthChangedBinding;
        
        protected abstract float HealthThreshold { get; }
        protected bool IsActive { get; private set; }

        protected AugmentHealthThresholdBase(SO_Augment augmentData, PlayerCharacter owner) : base(augmentData, owner)
        { }
        
        public override void Initialize()
        {
            _healthChangedBinding = new EventBinding<TriggerHealthChanged>(OnHealthChanged);
            EventBus<TriggerHealthChanged>.Register(_healthChangedBinding);
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

        public override void Dispose()
        {
            EventBus<TriggerHealthChanged>.Deregister(_healthChangedBinding);
        }
    }
}