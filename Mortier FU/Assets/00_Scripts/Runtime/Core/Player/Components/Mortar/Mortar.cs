using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class Mortar : MonoBehaviour
    {
        public Action<ShootMode> OnShootModeChanged;
        
        [Header("References")]
        [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        private Character character;
        private ShootMode _currentShootMode = ShootMode.PositionLimited;
        private MortarShootStrategy _shootStrategy;
        private CountdownTimer _shootTimer;
        private bool _isAiming;
        
        public SO_CharacterStats CharacterStats { get; private set; }
        public AimWidget AimWidget { get; private set; }
        
        // TODO: Remove direct dependency on PlayerInput
        private PlayerInput _playerInput;
        private InputAction _aimInputAction;
        private InputAction _shootInputAction;
        private InputAction _cycleShootStrategyAction;
        private InputAction _cycleShootModeAction;

        public ShootMode CurrentShootMode => _currentShootMode;
        
        public bool CanShoot => !_shootTimer.IsRunning;

        void Awake()
        {
            // TODO: Remove direct dependency on PlayerInput
            _playerInput = GetComponent<PlayerInput>();
            _aimInputAction = _playerInput.actions.FindAction("Aim");
            _shootInputAction = _playerInput.actions.FindAction("Shoot");
            _cycleShootModeAction = _playerInput.actions.FindAction("CycleShootMode");
        }

        void OnEnable()
        {
            _cycleShootModeAction.performed += ShootModeManager.CycleShootMode;
        }

        void OnDisable()
        {
            _cycleShootModeAction.performed -= ShootModeManager.CycleShootMode;
        }

        void Start()
        {
            if (!TryGetComponent(out character))
            {
                Logs.LogError("Mortar requires a Character component on the same GameObject.");
                return;
            }
            CharacterStats = character.CharacterStats;
            
            AimWidget = Instantiate(_aimWidgetPrefab);
            AimWidget.GetComponent<Renderer>().material.color = character.PlayerColor;
            
            SetShootMode(_currentShootMode);
            
            _shootTimer = new CountdownTimer(CharacterStats.AttackSpeed.Value);
            
            _shootInputAction.Disable();
        }
        
        private void OnDestroy()
        {
            _shootStrategy?.DeInitialize();
        }

        public void SetShootMode(ShootMode mode)
        {
            _shootStrategy?.DeInitialize();
            
            _currentShootMode = mode;
            _shootStrategy = MortarShootStrategyFactory.Create(_currentShootMode, this, 
                _aimInputAction, _shootInputAction);
            _shootStrategy.Initialize();
            
            OnShootModeChanged?.Invoke(_currentShootMode);
        }

        public void HandleAimMovement()
        {
            _shootStrategy?.Update();
            
            AimWidget.transform.localScale = Vector3.one * (CharacterStats.DamageRange.Value * 2);
        }
        
        public void Shoot()
        {
            if (_shootTimer.IsRunning) return;
            
            Bombshell.Data bombshellData = new Bombshell.Data
            {
                Owner = character,
                StartPos = _firePoint.position,
                TargetPos = AimWidget.transform.position,
                TravelTime = CharacterStats.ProjectileTimeTravel.Value,
                GravityScale = 1.0f,
                Damage = CharacterStats.Damage.Value,
                AoeRange = CharacterStats.DamageRange.Value
            };
            
            var bombshell = BombshellManager.Instance.RequestBombshell(bombshellData);
            
            // Reevaluates the attack speed every time we shoot. Not dynamic, could be improved ?
            _shootTimer.Reset(CharacterStats.AttackSpeed.Value);
            _shootTimer.Start();
        }

        public void BeginAiming()
        { 
            AimWidget.Show();
            _shootStrategy?.BeginAiming();
            _shootInputAction.Enable();
        }

        public void EndAiming()
        {
            AimWidget.Hide();
            _shootStrategy?.EndAiming();
            _shootInputAction.Disable();
        }
    }
}