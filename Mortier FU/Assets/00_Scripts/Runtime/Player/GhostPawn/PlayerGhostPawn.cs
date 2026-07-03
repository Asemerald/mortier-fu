using System.Collections.Generic;
using System.Linq;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerGhostPawn : MonoBehaviour, IPlayerPawn
    {
        [Header("References")]
        [SerializeField] private GameObject _rootVisual;

        private Rigidbody _rb;

        private GhostMovementComponent _movement;
        private GhostPropPlacementComponent _propPlacement;
        private GhostVisualComponent _visual;
        
        [SerializeField] private GhostAspectMaterials[] _ghostAspectMaterials;

        private bool _componentsCreated;

        public PlayerManager Owner { get; private set; }
        public PlayerCharacter SourceCharacter { get; private set; }
        public SO_GhostSettings Settings { get; private set; }
        
        public GhostAspectMaterials[] AspectMaterials => _ghostAspectMaterials;

        public Transform PawnTransform => transform;
        public bool IsPawnActive { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            CreateComponentsIfNeeded();
        }

        private void Update()
        {
            if (!IsPawnActive)
                return;

            _propPlacement?.Tick();
        }

        private void FixedUpdate()
        {
            if (!IsPawnActive)
                return;

            _movement?.FixedTick();
        }

        private void OnDestroy() => DisposeComponents();

        public void Initialize(PlayerManager owner, PlayerCharacter sourceCharacter, SO_GhostSettings settings)
        {
            Owner = owner;
            SourceCharacter = sourceCharacter;
            Settings = settings;

            if (!Owner)
            {
                Logs.LogError("[PlayerGhostPawn] Owner is null.", this);
                return;
            }

            if (!Settings)
            {
                Logs.LogError("[PlayerGhostPawn] Ghost settings are null.", this);
                return;
            }

            CreateComponentsIfNeeded();

            _movement.Initialize();
            _propPlacement.Initialize();
            _visual.Initialize();
            

            ExitPawn();
        }

        private void CreateComponentsIfNeeded()
        {
            if (_componentsCreated)
                return;

            _movement = new GhostMovementComponent(this, _rb);
            _propPlacement = new GhostPropPlacementComponent(this);
            _visual = new GhostVisualComponent(this, _rootVisual);

            _componentsCreated = true;
        }

        private void DisposeComponents()
        {
            _movement?.Dispose();
            _propPlacement?.Dispose();
            _visual?.Dispose();

            _movement = null;
            _propPlacement = null;
            _visual = null;

            _componentsCreated = false;
        }

        public void EnterPawn()
        {
            if (!Owner)
            {
                Logs.LogError("[PlayerGhostPawn] Cannot enter pawn without owner.", this);
                return;
            }

            if (!Settings)
            {
                Logs.LogError("[PlayerGhostPawn] Cannot enter pawn without settings.", this);
                return;
            }

            gameObject.SetActive(true);
            IsPawnActive = true;

            if (_rb)
            {
                _rb.useGravity = false;
                _rb.isKinematic = false;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _movement?.Reset();
            _propPlacement?.Reset();

            _movement?.OnEnterPawn();
            _propPlacement?.OnEnterPawn();

            _visual?.PlaySpawnFeedback();
        }

        public void ExitPawn()
        {
            IsPawnActive = false;

            _propPlacement?.OnExitPawn();
            _movement?.OnExitPawn();

            _movement?.Reset();
            _propPlacement?.Reset();

            _visual?.Hide();

            gameObject.SetActive(false);
        }

        public void SetMoveInput(Vector2 input)
        { }

        public void SetAimInput(Vector2 input)
        {
            _propPlacement?.SetAimInput(input);
        }

        public void SetAimHeld(bool isHeld)
        {
            _propPlacement?.SetAimHeld(isHeld);
        }

        public void ShootPressed()
        { }

        public void ShootReleased()
        {
            _propPlacement?.ShootReleased();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);

            if (_rb)
            {
                _rb.position = position;
                _rb.rotation = rotation;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();
        }
    }
}