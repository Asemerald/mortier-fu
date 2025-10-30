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
        
        private MortarShootStrategy _shootStrategy;
        private CountdownTimer _shootTimer;
        private bool _isAiming;

        public bool CanShoot => !_shootTimer.IsRunning;

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
            
            AimWidget = Object.Instantiate(aimWidgetPrefab); // Load via Addressable ?
            _firePoint = firePoint;
        }

        public override void Initialize()
        {
            // Find and cache Input Actions
            character.FindInputAction("Aim", out _aimAction);
            character.FindInputAction("Shoot", out _shootAction);

            // Temporary set the assigned color of the player to the aim widget
            if (AimWidget.TryGetComponent(out Renderer renderer))
            {
                renderer.material.color = character.PlayerColor;
            }

            _shootStrategy = new MSSPositionLimited(this, _aimAction, _shootAction);
            _shootTimer = new CountdownTimer(Stats.FireRate.Value);
            
            _shootStrategy.Initialize();
            _shootAction.Disable();
            
            // TODO: Feed this group from the GM
            // FindFirstObjectByType<CinemachineTargetGroup>().AddMember(transform, 1, 1);
        }

        public override void Reset()
        {
            _shootTimer.Stop();
        }
        
        public override void Dispose()
        {
            _shootTimer.Dispose();
            _shootStrategy?.DeInitialize();
        }

        public void HandleAimMovement()
        {
            _shootStrategy?.Update();
            
            AimWidget.transform.localScale = Vector3.one * (Stats.DamageRange.Value * 2);
        }
        
        public void Shoot()
        {
            if (_shootTimer.IsRunning) return;
            
            Bombshell.Data bombshellData = new Bombshell.Data
            {
                Owner = character,
                StartPos = _firePoint.position,
                TargetPos = AimWidget.transform.position,
                TravelTime = Stats.ProjectileTimeTravel.Value,
                GravityScale = 1.0f,
                Damage = Mathf.RoundToInt(Stats.DamageAmount.Value),
                AoeRange = Stats.DamageRange.Value
            };
            
            var bombshell = BombshellManager.Instance.RequestBombshell(bombshellData);
            
            // Reevaluates the attack speed every time we shoot. Not dynamic, could be improved ?
            _shootTimer.Reset(Stats.FireRate.Value);
            _shootTimer.Start();
        }

        public void BeginAiming()
        { 
            AimWidget.Show();
            _shootStrategy?.BeginAiming();
            _shootAction.Enable();
        }

        public void EndAiming()
        {
            AimWidget.Hide();
            _shootStrategy?.EndAiming();
            _shootAction.Disable();
        }
    }
}