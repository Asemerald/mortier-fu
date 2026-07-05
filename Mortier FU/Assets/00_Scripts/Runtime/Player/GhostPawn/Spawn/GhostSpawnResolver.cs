using UnityEngine;

namespace MortierFu
{
    public sealed class GhostSpawnResolver
    {
        private readonly SO_GhostSettings _settings;
        private readonly LevelSystem _levelSystem;

        public GhostSpawnResolver(SO_GhostSettings settings, LevelSystem levelSystem)
        {
            _settings = settings;
            _levelSystem = levelSystem;
        }

        public GhostSpawnResult Resolve(PlayerCharacter character, DeathContext context)
        {
            if (!character || !_settings)
                return new GhostSpawnResult(Vector3.zero, Quaternion.identity);

            if (context.Source is DeathTrigger deathTrigger)
            {
                GhostSpawnResult triggerResult = ResolveDeathTriggerSpawn(character, context, deathTrigger);
                return ValidateOrFallback(character, triggerResult, context);
            }

            if (context.DeathCause == E_DeathCause.VehicleCrash)
            {
                if (TryFindValidGroundNear(context.DeathPosition, out GhostSpawnResult vehicleResult))
                    return vehicleResult;

                if (TryGetLastSafeGround(character, out GhostSpawnResult safeResult))
                    return safeResult;

                return GetPlayerSpawnFallback(character);
            }

            if (context.DeathCause == E_DeathCause.BombshellExplosion)
            {
                if (TryProjectToValidGround(context.DeathPosition, out GhostSpawnResult mortarResult))
                    return mortarResult;

                return ValidateOrFallback(character, GetDefaultDeathResult(character, context), context);
            }

            return ValidateOrFallback(character, GetDefaultDeathResult(character, context), context);
        }

        private GhostSpawnResult ResolveDeathTriggerSpawn(PlayerCharacter character, DeathContext context, DeathTrigger deathTrigger)
        {
            if (deathTrigger.GhostSpawnPolicy == E_GhostSpawnPolicy.ExplicitAnchor && deathTrigger.GhostSpawnAnchor)
                return new GhostSpawnResult(deathTrigger.GhostSpawnAnchor.position, deathTrigger.GhostSpawnAnchor.rotation);

            if (TryGetLastSafeGround(character, out GhostSpawnResult safeResult))
                return safeResult;

            return GetPlayerSpawnFallback(character);
        }

        private GhostSpawnResult ValidateOrFallback(PlayerCharacter character, GhostSpawnResult preferredResult, DeathContext context)
        {
            if (TryProjectToValidGround(preferredResult.Position, out GhostSpawnResult validResult))
                return new GhostSpawnResult(validResult.Position, preferredResult.Rotation);

            if (context.DeathCause == E_DeathCause.VehicleCrash && TryFindValidGroundNear(context.DeathPosition, out GhostSpawnResult nearbyResult))
                return nearbyResult;

            if (TryGetLastSafeGround(character, out GhostSpawnResult safeResult))
                return safeResult;

            return GetPlayerSpawnFallback(character);
        }

        private GhostSpawnResult GetDefaultDeathResult(PlayerCharacter character, DeathContext context)
        {
            Quaternion rotation = Quaternion.Euler(0f, character.transform.eulerAngles.y, 0f);
            return new GhostSpawnResult(context.DeathPosition, rotation);
        }

        private bool TryGetLastSafeGround(PlayerCharacter character, out GhostSpawnResult result)
        {
            if (character.SafeGround != null && character.SafeGround.HasSafeGround)
            {
                result = new GhostSpawnResult(character.SafeGround.LastSafeGroundPosition, character.SafeGround.LastSafeGroundRotation);
                return true;
            }

            result = default;
            return false;
        }

        private GhostSpawnResult GetPlayerSpawnFallback(PlayerCharacter character)
        {
            PlayerManager owner = character ? character.Owner : null;
            Transform spawnPoint = _levelSystem?.GetSpawnPoint(owner ? owner.PlayerIndex : 0);

            if (spawnPoint && TryProjectToValidGround(spawnPoint.position, out GhostSpawnResult projectedResult))
                return new GhostSpawnResult(projectedResult.Position, spawnPoint.rotation);

            if (spawnPoint)
                return new GhostSpawnResult(spawnPoint.position, spawnPoint.rotation);

            return new GhostSpawnResult(Vector3.zero, Quaternion.identity);
        }

        private bool TryFindValidGroundNear(Vector3 origin, out GhostSpawnResult result)
        {
            if (TryProjectToValidGround(origin, out result))
                return true;

            int steps = Mathf.Max(4, _settings.VehicleSpawnSearchSteps);
            float maxRadius = Mathf.Max(0.25f, _settings.VehicleSpawnSearchRadius);

            for (int ring = 1; ring <= 3; ring++)
            {
                float radius = maxRadius * ring / 3f;

                for (int i = 0; i < steps; i++)
                {
                    float angle = i * Mathf.PI * 2f / steps;
                    Vector3 offset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    Vector3 candidate = origin + offset;

                    if (TryProjectToValidGround(candidate, out result))
                        return true;
                }
            }

            result = default;
            return false;
        }

        private bool TryProjectToValidGround(Vector3 position, out GhostSpawnResult result)
        {
            Vector3 rayStart = position + Vector3.up * _settings.GroundRaycastStartHeight;

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _settings.GroundRaycastLength, _settings.GroundMask, QueryTriggerInteraction.Ignore))
            {
                result = default;
                return false;
            }

            Vector3 groundPosition = hit.point;

            if (IsSpawnBlocked(groundPosition))
            {
                result = default;
                return false;
            }

            Quaternion rotation = Quaternion.Euler(0f, 0f, 0f);
            result = new GhostSpawnResult(groundPosition, rotation);
            return true;
        }

        private bool IsSpawnBlocked(Vector3 groundPosition)
        {
            Vector3 center = groundPosition + Vector3.up * _settings.FloatHeight;
            return Physics.CheckSphere(center, _settings.SpawnCheckRadius, _settings.GhostSpawnBlockingMask, QueryTriggerInteraction.Ignore);
        }
    }
}