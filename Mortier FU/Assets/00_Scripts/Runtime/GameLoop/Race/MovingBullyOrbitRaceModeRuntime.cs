using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(
        fileName = "DA_RaceMode_MovingBullyOrbit",
        menuName = "Mortier Fu/Race Modes/Moving Bully Orbit"
    )]
    public sealed class SO_MovingBullyOrbitRaceModeDefinition : SO_RaceModeDefinition
    {
        [Header("Orbit")]
        [Min(0f)] public float OrbitSpeedDegreesPerSecond = 90f;

        public override RaceModeRuntimeBase CreateRuntime() => new MovingBullyOrbitRaceModeRuntime();
    }
}