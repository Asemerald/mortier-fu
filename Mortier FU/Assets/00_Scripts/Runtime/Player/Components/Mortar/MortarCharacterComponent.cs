using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public class MortarCharacterComponent : CharacterComponent
    {
        public readonly AimWidget AimWidget;
        private readonly Transform _firePoint;

        private InputAction _aimAction, _shootAction;

        private BombshellSystem _bombshellSys;
        private MortarShootStrategy _shootStrategy;
        internal CountdownTimer _shootCooldownTimer;
        private CameraSystem _cameraSystem;
        private bool _isAiming;
        private bool _isInitialized;

        public bool CanShoot => _shootCooldownTimer != null && !_shootCooldownTimer.IsRunning;

        public float ShootCooldownProgress => _shootCooldownTimer?.Progress ?? 0f;

        public bool IsShooting { get; private set; }

        public MortarCharacterComponent(PlayerCharacter character, AimWidget aimWidgetPrefab, Transform firePoint) : base(character)
        {
            if (character == null) return;

            if (aimWidgetPrefab == null)
            {
                Logs.LogError("[MortarCharacterComponent]: AimWidget prefab is null!");
                return;
            }

            if (firePoint == null)
            {
                Logs.LogError("[MortarCharacterComponent]: FirePoint transform is null!");
                return;
            }

            AimWidget = Object.Instantiate(aimWidgetPrefab);
            AimWidget.name = $"Runtime AimWidget - {character.name}";
            AimWidget.Hide();

            _firePoint = firePoint;
        }

        public override void Initialize()
        {
            character.FindInputAction("Aim", out _aimAction);
            character.FindInputAction("Shoot", out _shootAction);

            _bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            if (_bombshellSys == null)
            {
                Logs.LogError("[MortarCharacterComponent]: Could not get the bombshell system from the system manager !");
                return;
            }

            _cameraSystem = SystemManager.Instance.Get<CameraSystem>();
            if (_cameraSystem == null)
            {
                Logs.LogError("[MortarCharacterComponent]: Could not get the camera system from the system manager !");
                return;
            }

            Color playerColor = character.Aspect.PlayerColor;
            
            AimWidget.Colorize(playerColor);
            
            ResetAimWidget();

            _shootStrategy = new MSSPositionLimited(this, _aimAction, _shootAction);
            _shootCooldownTimer = new CountdownTimer(Stats.GetFireRate());
            Stats.FireRate.OnDirtyUpdated += UpdateFireRate;

            _shootStrategy.Initialize();
            _shootAction.Disable();

            _shootCooldownTimer.OnTimerStop += OnShootCooldownComplete;
            
            _isInitialized = true;
        }

        public override void Update()
        {
            if (!_isInitialized || !AimWidget)
                return;

            AimWidget.UpdateFireRateProgress(1f - ShootCooldownProgress);
        }

        public override void Reset()
        {
            if (!_isInitialized)
                return;

            _shootCooldownTimer?.Reset();
            _shootCooldownTimer?.Stop();

            ResetAimWidget();
        }

        private void OnShootCooldownComplete()
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_Mortar_ReloadComplete, character.transform.position);
            Character.Aspect.ReloadCompleteFeedback();
        }

        private void UpdateFireRate()
        {
            float fireRate = Stats.GetFireRate();
            _shootCooldownTimer.DynamicUpdate(fireRate);
        }

        private void ResetAimWidget()
        {
            if (!AimWidget) return;
            
            AimWidget.transform.localScale = Vector3.one * (Stats.BombshellImpactRadius.Value * 2f);
            AimWidget.SetRelativePosition(Vector3.zero);
            AimWidget.Hide();
        }

        public override void Dispose()
        {
            if (_isInitialized)
            {
                Stats.FireRate.OnDirtyUpdated -= UpdateFireRate;

                if (_shootCooldownTimer != null)
                {
                    _shootCooldownTimer.OnTimerStop -= OnShootCooldownComplete;
                    _shootCooldownTimer.Dispose();
                }

                _shootStrategy?.DeInitialize();
            }

            _isInitialized = false;
        }

        public void HandleAimMovement()
        {
            if (!Character.CanAim)
                return;

            _shootStrategy?.Update();
        }

        public void Shoot()
        {
            if (!_isInitialized)
                return;

            if (!Character.CanShoot)
                return;

            if (_bombshellSys == null)
                return;

            if (_shootCooldownTimer == null || _shootCooldownTimer.IsRunning)
                return;

            AimWidget.RefreshPosition();

            Vector3 targetPosition = AimWidget.ShootTargetPosition;

            Bombshell.Data bombshellData = new Bombshell.Data
            {
                Owner = character,
                StartPos = _firePoint.position,
                TargetPos = targetPosition,
                Speed = Stats.GetBombshellSpeed(),
                GravityScale = 1.0f,
                Damage = Math.Max(1, Mathf.RoundToInt(Stats.BombshellDamage.Value)),
                Scale = Stats.GetBombshellSize(),
                AoeRange = Stats.BombshellImpactRadius.Value,
                Bounces = Mathf.RoundToInt(Stats.BombshellBounces.Value)
            };

            Bombshell bombshell = _bombshellSys.RequestBombshell(bombshellData);

            AudioService.PlayBombshellAudio(AudioService.FMODEvents.SFX_Mortar_Shot, bombshell, character.transform.position);

            EventBus<TriggerShootBombshell>.Raise(new TriggerShootBombshell()
            {
                Character = character,
                Bombshell = bombshell,
            });

            _shootCooldownTimer.Start();

            IsShooting = true;
        }

        public void StopShooting() => IsShooting = false;

        public void BeginAiming(InputAction.CallbackContext ctx)
        {
            if (character.Owner != null && character.Owner.IsControllingGhost)
                return;
            
            if (!_isInitialized)
                return;

            if (!Character.CanAim)
                return;

            if (!Character.Health.IsAlive)
                return;

            if (AimWidget == null || _shootAction == null)
                return;

            AimWidget.Show();
            _shootStrategy?.BeginAiming();
            _shootAction.Enable();
        }

        public void EndAiming(InputAction.CallbackContext ctx)
        {
            if (character.Owner != null && character.Owner.IsControllingGhost)
                return;
            
            if (!_isInitialized)
                return;

            if (AimWidget)
                AimWidget.Hide();
            
            _shootStrategy?.EndAiming();

            if (_shootAction != null)
                _shootAction.Disable();
        }
        
        public void CancelAiming()
        {
            if (character.Owner != null && character.Owner.IsControllingGhost)
                return;

            if (AimWidget)
                AimWidget.Hide();
            

            _shootStrategy?.CancelAiming();

            _shootAction?.Disable();

            IsShooting = false;
        }
    }
}