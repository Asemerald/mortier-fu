using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Helpers.Runtime.Shading;
using MortierFu;
using UnityEngine;

public class GrassInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Renderer grassRenderer;
    [SerializeField] private float duration;
    
    public bool IsDashInteractable { get; } = false;
    public bool IsBombshellInteractable { get; private set; } = true;

    private static readonly int _shaderGrassParameters = Shader.PropertyToID("_Progression");

    private const float shaderGrassBurnValue = 1f;
    private const float shaderGrassBaseValue = -0.1f;
    
    private CancellationTokenSource _cts;

    private void Awake()
    {
        grassRenderer.SetFloatProperty(_shaderGrassParameters,shaderGrassBaseValue);
        
        _cts = new CancellationTokenSource();
    }

    public void Interact(Vector3 contactPoint)
    {
        if (!IsBombshellInteractable) return;
        IsBombshellInteractable = false; 
        LerpShaderGrass(_cts.Token).Forget();
    }

    private async UniTask LerpShaderGrass(CancellationToken token)
    {
        if (duration <= 0f) 
        {
            grassRenderer.SetFloatProperty(_shaderGrassParameters,shaderGrassBurnValue);
            return;
        }
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            
            float ratio = Mathf.Lerp(shaderGrassBaseValue, shaderGrassBurnValue, elapsedTime / duration);
            grassRenderer.SetFloatProperty(_shaderGrassParameters,ratio);
            await UniTask.Yield(PlayerLoopTiming.Update,token);
        }
        
        grassRenderer.SetFloatProperty(_shaderGrassParameters,shaderGrassBurnValue);
    }
    
}
