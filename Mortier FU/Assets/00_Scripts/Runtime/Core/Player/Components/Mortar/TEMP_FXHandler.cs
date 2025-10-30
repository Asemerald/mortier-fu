using System.Numerics;
using UnityEngine;
using MortierFu.Shared;
using PrimeTween;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class TEMP_FXHandler : MonoBehaviour
{
    public static TEMP_FXHandler Instance { get; private set; }
    
    [SerializeField] private ParticleSystem _bombshellPreview;
    [SerializeField] private ParticleSystem _bombshellExplosion;
    [SerializeField] private ParticleSystem _strike;
    
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } Instance = this;
    }

    public void InstantiatePreview(Vector3 position, float travelTime, float range)
    {
        ParticleSystem preview = Instantiate(_bombshellPreview, position + new Vector3(0, 0.1f, 0), Quaternion.identity);
        preview.transform.localScale = Vector3.zero;
        Tween.Scale(preview.transform, Vector3.one * (range * 2), duration: 0.5f, ease: Ease.OutCubic);
        var main = preview.main;
        main.simulationSpeed = 1/travelTime;
    }
    
    public void InstantiateExplosion(Vector3 position, float range)
    {
        ParticleSystem preview = Instantiate(_bombshellExplosion, new Vector3(position.x, 0.5f, position.z), Quaternion.identity);
        preview.transform.localScale = Vector3.one * range *0.8f;
    }

    public void InstantiateStrikeFX(Transform player, float size)
    {
        ParticleSystem strikeFX = Instantiate(_strike, player);
        strikeFX.transform.localScale = Vector3.one * size;
    }

}
