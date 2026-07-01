using UnityEngine;
using UnityEngine.Serialization;
namespace MortierFu
{
    public class AGM_Berserker : AugmentBase
    {
        [System.Serializable]
        public struct Params
        {
            [Min(0f)] public float ThresholdTimer;
            public AugmentStatMod BombshellDamageMod;
            public AugmentStatMod MoveSpeedMod;
            public AugmentStatMod FireRateMod;

			public AugmentStatMod BombshellDamageModPreproc;
            public AugmentStatMod MoveSpeedModPreproc;
            public AugmentStatMod FireRateModPreproc;
        }

        private CountdownTimer _thresholdTimer;
        private EventBinding<TriggerEndRound> _endRoundBinding;
        private IGameMode _gameMode;
        private bool _isActive;
        
        public AGM_Berserker(SO_Augment augmentData, PlayerCharacter owner, SO_AugmentDatabase db) : base(augmentData, owner, db)
        { }

        public override void Initialize()
        {
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageModPreproc.ToMod(this));
            stats.FireRate.AddModifier(db.BerserkerParams.FireRateModPreproc.ToMod(this));
            stats.MoveSpeed.AddModifier(db.BerserkerParams.MoveSpeedModPreproc.ToMod(this));

            _endRoundBinding = new EventBinding<TriggerEndRound>(OnEndRound);
            EventBus<TriggerEndRound>.Register(_endRoundBinding);

            _gameMode = GameService.CurrentGameMode;
            if (_gameMode != null)
            {
                _gameMode.OnRoundStarted += OnRoundStarted;
            }
        }

        private void OnRoundStarted(RoundInfo roundInfo)
        {
            StartThresholdTimer();
        }

        private void StartThresholdTimer()
        {
            ResetToPreproc();
            StopThresholdTimer();

            float duration = db.BerserkerParams.ThresholdTimer;
            if (duration <= 0f)
            {
                ActivateThreshold();
                return;
            }

            _thresholdTimer = new CountdownTimer(duration);
            _thresholdTimer.OnTimerStop += OnThresholdTimerStopped;
            _thresholdTimer.Start();
        }

        private void OnThresholdTimerStopped()
        {
            if (_thresholdTimer == null || _thresholdTimer.CurrentTime > 0f || _isActive)
                return;

            ActivateThreshold();
        }

        private void ActivateThreshold()
        {
            _isActive = true;
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageMod.ToMod(this));
            stats.MoveSpeed.AddModifier(db.BerserkerParams.MoveSpeedMod.ToMod(this));
            stats.FireRate.AddModifier(db.BerserkerParams.FireRateMod.ToMod(this));
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Augment_Buff, owner.transform.position);
        }

        private void OnEndRound(TriggerEndRound evt)
        {
            StopThresholdTimer();
            ResetToPreproc();
        }

        private void StopThresholdTimer()
        {
            if (_thresholdTimer != null)
            {
                _thresholdTimer.OnTimerStop -= OnThresholdTimerStopped;
                _thresholdTimer.Stop();
                _thresholdTimer.Dispose();
                _thresholdTimer = null;
            }
        }

        private void ResetToPreproc()
        {
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
            stats.BombshellDamage.AddModifier(db.BerserkerParams.BombshellDamageModPreproc.ToMod(this));
            stats.FireRate.AddModifier(db.BerserkerParams.FireRateModPreproc.ToMod(this));
            stats.MoveSpeed.AddModifier(db.BerserkerParams.MoveSpeedModPreproc.ToMod(this));
            _isActive = false;
        }

        public override void Dispose()
        {
            if (_gameMode != null)
            {
                _gameMode.OnRoundStarted -= OnRoundStarted;
                _gameMode = null;
            }

            EventBus<TriggerEndRound>.Deregister(_endRoundBinding);
            StopThresholdTimer();
            stats.BombshellDamage.RemoveAllModifiersFromSource(this);
            stats.MoveSpeed.RemoveAllModifiersFromSource(this);
            stats.FireRate.RemoveAllModifiersFromSource(this);
            base.Dispose();
        }
    }
}