using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using PrimeTween;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class TEMP_FXHandler : MonoBehaviour
{
    public static TEMP_FXHandler Instance { get; private set; }
    
    [SerializeField] private ParticleSystem _bombshellPreview;
    [SerializeField] private ParticleSystem[] _bombshellExplosionColors;
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

        StrikeTimingPreview(preview, travelTime).Forget();
    }

    private async UniTaskVoid StrikeTimingPreview(ParticleSystem preview, float travelTime)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, travelTime - 0.3f)));

        var col = preview.colorOverLifetime;

        //COLOR CHANGES
        Gradient grad = new Gradient();
        grad.SetKeys( new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 0.6f), new GradientColorKey(Color.white, 1.0f) }, 
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0.45f, 0.06f), new GradientAlphaKey(0.40f, 0.7f), new GradientAlphaKey(0.35f, 1.0f) } );

        col.color = grad;
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

    public void InstantiateStrikeFX(Transform player, float size)
    {
        ParticleSystem strikeFX = Instantiate(_strike, player);
        strikeFX.transform.localScale = Vector3.one * size;
    }

}
