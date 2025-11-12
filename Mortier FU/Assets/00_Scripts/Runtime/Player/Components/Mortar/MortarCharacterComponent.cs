using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class MortarCharacterComponent : CharacterComponent
    {
        public readonly AimWidget AimWidget;
        private readonly Transform _firePoint;
        
        private InputAction _aimAction, _shootAction;
        
        private BombshellSystem _bombshellSys;
        private MortarShootStrategy _shootStrategy;
        private CountdownTimer _shootCooldownTimer;
        private bool _isAiming;

        public bool CanShoot => !_shootCooldownTimer.IsRunning;

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

            AimWidget = Object.Instantiate(aimWidgetPrefab); // TODO Load via Addressable?
            _firePoint = firePoint;
        }

        public override void Initialize()
        {
            // Find and cache Input Actions
            character.FindInputAction("Aim", out _aimAction);
            character.FindInputAction("Shoot", out _shootAction);
            
            _bombshellSys = SystemManager.Instance.Get<BombshellSystem>();
            if (_bombshellSys == null)
            {
                Logs.LogError("[MortarCharacterComponent]: Could not get the bombshell system from the system manager !");
                return;
            }

            Color playerColor = character.Aspect.PlayerColor;
            AimWidget.Colorize(playerColor);
            ResetAimWidget();
            
            _shootStrategy = new MSSPositionLimited(this, _aimAction, _shootAction);
            _shootCooldownTimer = new CountdownTimer(Stats.FireRate.Value);
            
            _shootStrategy.Initialize();
            _shootAction.Disable();
            
            Object.FindFirstObjectByType<TEMP_CameraHandler>()._targetGroup.AddMember(character.transform, 1, 1); // TODO better when refacto CameraHandler
        }

        public override void Reset()
        {
            _shootCooldownTimer?.Stop();
            
            _shootTimer?.Stop();
            ResetAimWidget();

            AimWidget.SetRelativePosition(Vector3.zero);
        }

        private void ResetAimWidget() {
            float damageScale = (Stats.DamageAmount.Value * Stats.DamageAmount.Value / 10) * 0.2f;
            float aoeRange = Stats.DamageRange.Value + damageScale * 0.7f;
            AimWidget.transform.localScale = Vector3.one * (aoeRange * 2f);   
            AimWidget.Hide();
        }

        public override void Dispose()
        {
            _shootCooldownTimer.Dispose();
            _shootStrategy?.DeInitialize();
        }

        public void HandleAimMovement()
        {
            _shootStrategy?.Update();
        }
        
        public void Shoot()
        {
            if (_shootCooldownTimer.IsRunning) return;

            float damageScale = (Stats.DamageAmount.Value * Stats.DamageAmount.Value / 10) * 0.2f;
            
            Bombshell.Data bombshellData = new Bombshell.Data
            {
                Owner = character,
                StartPos = _firePoint.position,
                TargetPos = AimWidget.transform.position,
                TravelTime = Stats.BombshellTimeTravel.Value,
                GravityScale = 1.0f,
                Damage = Mathf.RoundToInt(Stats.DamageAmount.Value),
                Scale =  Stats.BombshellSize.Value * (1 + damageScale),
                AoeRange = Stats.DamageRange.Value + damageScale * 0.7f
            };
            
            var bombshell = _bombshellSys.RequestBombshell(bombshellData);
            
            // Reevaluates the attack speed every time we shoot. Not dynamic, could be improved ?
            _shootCooldownTimer.Reset(Stats.FireRate.Value);
            _shootCooldownTimer.Start();
            
            IsShooting = true;
        }
        
        public void StopShooting() => IsShooting = false;

        public void BeginAiming(InputAction.CallbackContext ctx)
        { 
            Logs.Log("[MortarCharacterComponent]: Begin Aiming");
            AimWidget.Show();
            _shootStrategy?.BeginAiming();
            _shootAction.Enable();
        }

        public void EndAiming(InputAction.CallbackContext ctx)
        {
            Logs.Log("[MortarCharacterComponent]: End Aiming");
            AimWidget.Hide();
            _shootStrategy?.EndAiming();
            _shootAction.Disable();
        }
    }
}