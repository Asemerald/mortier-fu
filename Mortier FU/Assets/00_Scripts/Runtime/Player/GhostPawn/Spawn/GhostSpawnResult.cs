using UnityEngine;

namespace MortierFu
{
    public readonly struct GhostSpawnResult
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public GhostSpawnResult(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}