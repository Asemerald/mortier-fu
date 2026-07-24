using UnityEngine;

public class SimplePropPhysic : AbstractPropPhysic
{
    [Header("Prop Settings")]
    [SerializeField] private float spawnOffsetY;

    protected override float SpawnOffsetY => spawnOffsetY;
}