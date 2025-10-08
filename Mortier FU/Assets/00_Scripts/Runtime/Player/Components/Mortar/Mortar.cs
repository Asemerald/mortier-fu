using System;
using MortierFu.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MortierFu
{
    public class Mortar : MonoBehaviour
    {
        public Action<ShootMode> OnShootModeChanged;
        
        [Header("Statistics")]
        public CharacterStat AttackSpeed = new CharacterStat(2.0f);
        public CharacterStat ShotRange = new CharacterStat(20.0f);
        [field: SerializeField] public float AimWidgetSpeed { get; private set; } = 7.0f;
        
        [Header("References")]
        [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        private ShootMode _currentShootMode = ShootMode.PositionLimited;
        private MortarShootStrategy _shootStrategy;
        private CountdownTimer _shootTimer;
        public AimWidget AimWidget { get; private set; }
        private bool _isAiming;
        
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
            
            AimWidget = Instantiate(_aimWidgetPrefab);
            SetShootMode(_currentShootMode);
            
            _shootTimer = new CountdownTimer(AttackSpeed.Value);
        }

        void OnEnable()
        {
            _cycleShootModeAction.performed += ShootModeManager.CycleShootMode;
        }

        void OnDisable()
        {
            _cycleShootModeAction.performed -= ShootModeManager.CycleShootMode;
        }

        private void OnDestroy()
        {
            _shootStrategy?.DeInitialize();
        }

        public void SetShootMode(ShootMode mode)
        {
            _shootStrategy?.DeInitialize();
            
            _currentShootMode = mode;
            _shootStrategy = MortarShootStrategyFactory.Create(_currentShootMode, this, _aimInputAction,_shootInputAction);
            _shootStrategy.Initialize();
            
            OnShootModeChanged?.Invoke(_currentShootMode);
        }

        void Update()
        {
            _shootStrategy?.Update();
        }
        
        public void Shoot()
        {
            if (_shootTimer.IsRunning) return;

            var owner = GetComponent<Character>();
            var bombshell = BombshellManager.Instance.RequestBombshell(owner, 100, 2.0f, 8.0f,
                1.0f, _firePoint.position, AimWidget.transform.position);
            
            _shootTimer.Start();
        }
    }
}