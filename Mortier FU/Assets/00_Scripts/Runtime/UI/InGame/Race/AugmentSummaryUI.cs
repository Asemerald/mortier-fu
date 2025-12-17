using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class AugmentSummaryUI : MonoBehaviour
    {
        [SerializeField] private Image[] _playerImages;

        private Tween _tween;
        
        public async UniTask AnimatePlayerImages(int playerCount)
        {
            for (int i = 0; i < _playerImages.Length; i++)
            {
                bool active = i < playerCount;
                _playerImages[i].gameObject.SetActive(active);

                if (active)
                    _playerImages[i].transform.localScale = Vector3.zero;
            }
            
            for (int i = 0; i < playerCount; i++)
            {
                _tween = Tween.Scale(
                    _playerImages[i].transform,
                    Vector3.zero,
                    Vector3.one * 0.65f,
                    0.4f,
                    Ease.OutBack
                );

                await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
            }
            
            await _tween;
        }
    }
}