using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using PrimeTween;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class TEMP_FXHandler : MonoBehaviour
{
    public static TEMP_FXHandler Instance { get; private set; }

    [SerializeField] private ParticleSystem _bombshellPreview;
    [SerializeField] private ParticleSystem[] _bombshellExplosionColors;
    [SerializeField] private ParticleSystem _dash;
    [SerializeField] private ParticleSystem _bombshellWaterExplosion;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InstantiatePreview(Vector3 position, float travelTime, float range)
    {
        ParticleSystem preview =
            Instantiate(_bombshellPreview, position + new Vector3(0, 0.1f, 0), Quaternion.identity);
        preview.transform.localScale = Vector3.zero;
        Tween.Scale(preview.transform, Vector3.one * (range * 2), duration: travelTime * 0.9f, ease: Ease.OutQuad);
        var main = preview.main;
        main.simulationSpeed = 1 / travelTime;
    }

    public void InstantiateExplosion(Vector3 position, float range, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _bombshellExplosionColors.Length)
        {
            Debug.LogError($"Player index {playerIndex} is out of range for bombshell explosion colors.");
            return;
        }

        var ps = Instantiate(_bombshellExplosionColors[playerIndex], position, Quaternion.identity);
        ps.transform.localScale = Vector3.one * (range * 0.5f);
    }

    public void InstantiateDashFX(Transform strikePoint, float size)
    {
        ParticleSystem strikeFX = Instantiate(_dash, strikePoint);
        strikeFX.transform.localScale = Vector3.one * size;
    }

    public void InstantiateWaterExplosionFX(Vector3 hitPoint)
    {
        var ps = Instantiate(_bombshellWaterExplosion, hitPoint, _bombshellWaterExplosion.transform.rotation);
        
        Destroy(ps.gameObject, ps.main.duration);
    }
}