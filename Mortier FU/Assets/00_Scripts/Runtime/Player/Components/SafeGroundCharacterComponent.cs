using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class SafeGroundCharacterComponent : CharacterComponent
    {
        private const int k_overlapCapacity = 16;
        private readonly Collider[] _overlapBuffer = new Collider[k_overlapCapacity];

        private GhostSystem _ghostSystem;

        public bool HasSafeGround { get; private set; }
        public Vector3 LastSafeGroundPosition { get; private set; }
        public Quaternion LastSafeGroundRotation { get; private set; }

        public SafeGroundCharacterComponent(PlayerCharacter character) : base(character) { }

        public override void Initialize()
        {
            _ghostSystem = SystemManager.Instance.Get<GhostSystem>();

            if (_ghostSystem == null)
                Logs.LogWarning("[SafeGroundCharacterComponent] GhostSystem not found.", character);
        }

        public override void Update()
        {
            if (!character || character.Health == null || !character.Health.IsAlive)
                return;

            if (_ghostSystem == null || !_ghostSystem.Settings)
                return;

            TryRefreshSafeGround();
        }

        private void TryRefreshSafeGround()
        {
            Vector3 origin = character.FeetPoint ? character.FeetPoint.position : character.transform.position;

            if (IsInsideDeathTrigger(origin))
                return;

            SO_GhostSettings settings = _ghostSystem.Settings;
            Vector3 rayStart = origin + Vector3.up * settings.GroundRaycastStartHeight;

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, settings.GroundRaycastLength, settings.GroundMask, QueryTriggerInteraction.Ignore))
                return;

            LastSafeGroundPosition = hit.point;
            LastSafeGroundRotation = Quaternion.Euler(0f, character.transform.eulerAngles.y, 0f);
            HasSafeGround = true;
        }

        private bool IsInsideDeathTrigger(Vector3 origin)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, 0.35f, _overlapBuffer, ~0, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlapBuffer[i];

                if (hit && hit.GetComponentInParent<DeathTrigger>())
                    return true;
            }

            return false;
        }
    }
}