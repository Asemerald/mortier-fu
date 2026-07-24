using MortierFu;
using UnityEngine;

public class GhostProp : AbstractPropPhysic
{
    [Header("Ghost Prop Settings")]
    [SerializeField] private SO_GhostPlaceableProp settings;

    protected override float SpawnOffsetY => settings.SpawnOffset.y;

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        if (!settings) return;
        base.OnDrawGizmosSelected();
    }
#endif
}