using UnityEngine;

namespace MortierFu
{
    public class AGM_EpicDash : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            public float EpicDashDuration;
            public AugmentStatMod MoveSpeedMod;
            public AugmentStatMod DashCooldownMod;
        }
        
        private const float k_maxBuffDurationToCooldownRatio = 0.85f;
        
        private CountdownTimer _epicDashTimer;
        private EventBinding<TriggerEndDash> _endDashBinding;
        private EventBinding<TriggerDash> _dashBinding;

        public AGM_EpicDash(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.DashCooldown.AddModifier(db.EpicDashParams.DashCooldownMod.ToMod(this));

            _epicDashTimer = new CountdownTimer(GetBuffDuration());
            _epicDashTimer.OnTimerStop += OnEpicDashTimerStop;

            _endDashBinding = new EventBinding<TriggerEndDash>(OnEndDash);
            EventBus<TriggerEndDash>.Register(_endDashBinding);

            _dashBinding = new EventBinding<TriggerDash>(OnDash);
            EventBus<TriggerDash>.Register(_dashBinding);
        }

        private void OnEndDash(TriggerEndDash evt)
        {
            if (evt.Character != owner)
                return;

            RefreshMoveSpeedBuff();
        }

        private void OnDash(TriggerDash evt)
        {
            HideVFX();
            ShowVFX();
        }

        private void RefreshMoveSpeedBuff()
        {
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.AddModifier(db.EpicDashParams.MoveSpeedMod.ToMod(this));

            _epicDashTimer.Stop();
            _epicDashTimer.Reset();
            _epicDashTimer.DynamicUpdate(GetBuffDuration());
            _epicDashTimer.Start();
        }

        private void OnEpicDashTimerStop()
        {
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
        }

        private float GetBuffDuration()
        {
            float dashCooldown = stats.GetDashCooldownDuration();
            float maxDuration = dashCooldown * k_maxBuffDurationToCooldownRatio;

            return Mathf.Clamp(db.EpicDashParams.EpicDashDuration, 0f, maxDuration);
        }

        public override void Dispose()
        {
            if (_endDashBinding != null)
                EventBus<TriggerEndDash>.Deregister(_endDashBinding);

            if (_dashBinding != null)
            {
                EventBus<TriggerDash>.Deregister(_dashBinding);
            }

            if (_epicDashTimer != null)
            {
                _epicDashTimer.OnTimerStop -= OnEpicDashTimerStop;
                _epicDashTimer.Stop();
                _epicDashTimer.Dispose();
                _epicDashTimer = null;
            }

            stats.DashCooldown.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            HideVFX();
        }
    }
}