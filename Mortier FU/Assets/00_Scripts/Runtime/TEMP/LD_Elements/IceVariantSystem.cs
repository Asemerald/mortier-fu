using MortierFu.Shared;
using UnityEngine;


public sealed class IceVariantSystem : MonoBehaviour
{
    
    [SerializeField] private GameObject iceVariant;
    [field: SerializeField, Range(0,1)] private float _iceProbability;
    
    
    private void OnEnable()
    {
        if (!iceVariant)
        {
            Logs.LogError($"[TEMP_IceVariantSystem] No iceVariant found in {gameObject.scene.name}.");
            return;
        }
        
        iceVariant.SetActive(ShouldActiveIce());
    }

    private void OnDisable()
    {
        if (!iceVariant)
        {
            Logs.LogError($"[TEMP_IceVariantSystem] No iceVariant found in {gameObject.scene.name}.");
            return;
        }
        
        iceVariant.SetActive(false);
    }

    private bool ShouldActiveIce()
    {
        return Random.value <= _iceProbability;
    }
}
