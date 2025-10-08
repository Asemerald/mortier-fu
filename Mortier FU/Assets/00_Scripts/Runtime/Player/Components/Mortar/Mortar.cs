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
        [SerializeField] private BombshellManager _bombshellManager;
        [SerializeField] private AimWidget _aimWidgetPrefab;
        [SerializeField] private Transform _firePoint;

        [Header("Debugging")] 
        [SerializeField] private bool _enableDebug = true;

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
            _cycleShootModeAction.performed += CycleShootMode;
        }

        void OnDisable()
        {
            _cycleShootModeAction.performed -= CycleShootMode;
        }

        private void OnDestroy()
        {
            _shootStrategy?.DeInitialize();
        }

        private void CycleShootMode(InputAction.CallbackContext obj)
        {
            int modeCount = Enum.GetValues(typeof(ShootMode)).Length;
            var newShootMode = (ShootMode)(((int)_currentShootMode + 1) % modeCount);
            SetShootMode(newShootMode);
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
            var bombshell = _bombshellManager.RequestBombshell(owner, 100, 2.0f, 8.0f,
                1.0f, _firePoint.position, AimWidget.transform.position);
            
            _shootTimer.Start();
        }
        
        static Vector3 PositionAtTime(Vector3 start, Vector3 v0, float t, Vector3 g)
        {
            return start + v0 * t + g * (0.5f * t * t);
        }

        // helper: compute v0 for a chosen absolute time T
        static Vector3 InitialVelocityForTime(Vector3 start, Vector3 target, float T, Vector3 g)
        {
            if (T <= 0f) return Vector3.zero;
            return (target - start - g * (0.5f * T * T)) / T;
        }
    }
}