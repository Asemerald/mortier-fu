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
        
        private Animator _playerAnimation;

        private void Awake()
        {
            // TODO: Add player animation component check
            /*playerAnimationPrefab.TryGetComponent(out _playerAnimation);
            
            if (_playerAnimation == null)
            {
                Logs.LogError("[MainMenuComposing]: Player Animation component is missing on the playerAnimationPrefab.", playerAnimationPrefab);
            }*/
        }

        private void Start()
        {
            PlayStartupAnimation().Forget();
        }
        
        private async UniTaskVoid PlayStartupAnimation()
        {
            //_playerAnimation.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(explosionDelay));

            if (ExplosionEffectPrefab != null)
            {
                Logs.LogWarning("[MainMenuComposing]: ExplosionEffectPrefab is not assigned.", this);
                return;
            }
            // TODO: Instantiate(ExplosionEffectPrefab, playerAnimationPrefab.transform.position, Quaternion.identity);
        }
        
    } 
}
