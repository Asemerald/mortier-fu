using System;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class MainMenuComposing : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject playerAnimationPrefab;
        [SerializeField] private GameObject ExplosionEffectPrefab;
        
        [Header("Startup Animation Settings")]
        [SerializeField] private float explosionDelay = 1.0f;
        
        private Animation _playerAnimation;

        private void Awake()
        {
            playerAnimationPrefab.TryGetComponent(out  _playerAnimation);
            
            if (_playerAnimation == null)
            {
                Logs.LogError("[MainMenuComposing]: Player Animation component is missing on the playerAnimationPrefab.", playerAnimationPrefab);
            }
        }

        private void Start()
        {
            PlayStartupAnimation().Forget();
        }
        
        private async UniTaskVoid PlayStartupAnimation()
        {
            _playerAnimation.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(explosionDelay));
            Instantiate(ExplosionEffectPrefab, playerAnimationPrefab.transform.position, Quaternion.identity);
        }
        
    } 
}
