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
        
        public DA_CharacterStats CharacterStats { get; private set; }
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
                Logs.Error("Mortar requires a Character component on the same GameObject.");
                return;
            }
            CharacterStats = character.CharacterStats;
            
            AimWidget = Instantiate(_aimWidgetPrefab);
            AimWidget.GetComponent<Renderer>().material.color = character.PlayerColor;
            
            SetShootMode(_currentShootMode);
            
            _shootTimer = new CountdownTimer(CharacterStats.AttackSpeed.Value);
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

        void Update()
        {
            _shootStrategy?.Update();
        }
        
        public void Shoot()
        {
            if (_shootTimer.IsRunning) return;

            float damage = CharacterStats.Damage.Value;
            float aoeRange = CharacterStats.AOERange.Value;
            float projectileSpeed = CharacterStats.ProjectileSpeed.Value;
            var bombshell = BombshellManager.Instance.RequestBombshell(character, damage, aoeRange, projectileSpeed,
                1.0f, _firePoint.position, AimWidget.transform.position);
            
            _shootTimer.Start();
        }
    }
}