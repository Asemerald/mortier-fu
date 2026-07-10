using MortierFu.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MortierFu
{
    public sealed class MovingBullyOrbitRaceModeRuntime : RaceModeRuntimeBase
    {
        private Transform _orbitRoot;

        private SO_MovingBullyOrbitRaceModeDefinition MovingDefinition => Definition as SO_MovingBullyOrbitRaceModeDefinition;

        public override Transform ResolveSpawnPoint(PlayerTeam team, PlayerManager player, int racerIndex, Transform fallback)
        {
            if (!Reporter)
                return fallback;

            if (IsBully(player))
                return Reporter.BullySpawnPoint ? Reporter.BullySpawnPoint : fallback;

            Transform racerSpawn = Reporter.GetRacerSpawnPoint(racerIndex);
            return racerSpawn ? racerSpawn : fallback;
        }

        public override RaceAugmentLayout BuildAugmentLayout(int augmentCount)
        {
            if (!Reporter || augmentCount <= 0)
                return base.BuildAugmentLayout(augmentCount);

            PlayerCharacter bullyCharacter = BullyCharacter;

            if (!bullyCharacter)
            {
                Logs.LogWarning("[MovingBullyOrbitRaceModeRuntime] Missing bully character. Falling back to LevelReporter layout.");
                return base.BuildAugmentLayout(augmentCount);
            }

            Transform orbitRoot = GetOrCreateOrbitRoot(bullyCharacter.transform);
            Vector3[] points = new Vector3[augmentCount];

            if (!Reporter.TryPopulateCircleAround(orbitRoot.position, points))
                return base.BuildAugmentLayout(augmentCount);

            return new RaceAugmentLayout(orbitRoot, points, parentPointsToPivot: true, useRotatorPrediction: false);
        }

        public override void Tick(float deltaTime)
        {
            if (!_orbitRoot || !BullyCharacter)
                return;

            _orbitRoot.position = BullyCharacter.transform.position;

            float speed = MovingDefinition ? MovingDefinition.OrbitSpeedDegreesPerSecond : 90f;

            if (speed <= 0f)
                return;

            _orbitRoot.Rotate(Vector3.up, speed * deltaTime, Space.World);
        }

        public override void End()
        {
            base.End();
            DestroyOrbitRoot();
        }

        public override void Dispose()
        {
            DestroyOrbitRoot();
        }

        private Transform GetOrCreateOrbitRoot(Transform bullyTransform)
        {
            if (_orbitRoot)
                return _orbitRoot;

            GameObject root = new ("Moving Bully Augment Orbit Root");
            root.transform.SetPositionAndRotation(bullyTransform.position, Quaternion.identity);

            root.transform.SetParent(bullyTransform, worldPositionStays: true);

            _orbitRoot = root.transform;
            return _orbitRoot;
        }

        private void DestroyOrbitRoot()
        {
            if (!_orbitRoot)
                return;

            Object.Destroy(_orbitRoot.gameObject);
            _orbitRoot = null;
        }
    }
}
