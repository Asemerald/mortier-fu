using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace MortierFu
{
    public class AugmentSummaryUI : MonoBehaviour
    {
        [SerializeField] private Image[] _playerImages;
        
        [Header("Settings")]
        [SerializeField] private float _playerScaleDuration = 0.4f;
        [SerializeField] private float _playerTargetScale = 0.65f;
        [SerializeField] private float _playerAnimDelay = 0.15f;
        [SerializeField] private Ease _playerScaleEase = Ease.OutBack;

        [SerializeField] private float _childRadius = 225f;
        [SerializeField] private float _childAnimDelay = 0.3f;
        [SerializeField] private float _childAnimDuration = 0.8f;
        [SerializeField] private Ease _augmentIconScaleEase = Ease.OutBack;
        [SerializeField] private Ease _augmentIconMoveEase = Ease.OutCubic;

        [SerializeField] private float _finalPauseDuration = 1.5f;

        private Tween _tween;

        public async UniTask AnimatePlayerImagesWithAugments(List<List<SO_Augment>> playerAugments)
        {
            int playerCount = playerAugments.Count;

            for (int i = 0; i < _playerImages.Length; i++)
            {
                bool active = i < playerCount;
                _playerImages[i].gameObject.SetActive(active);
                if (!active) continue;

                _playerImages[i].transform.localScale = Vector3.zero;

                int childCount = _playerImages[i].transform.childCount;
                for (int c = 0; c < childCount; c++)
                {
                    Transform child = _playerImages[i].transform.GetChild(c);
                    child.localScale = Vector3.zero;
                    child.localPosition = Vector3.zero;

                    var augments = playerAugments[i];
                    if (c < augments.Count)
                    {
                        var iconImg = child.GetComponent<Image>();
                        if (iconImg != null)
                        {
                            iconImg.sprite = augments[augments.Count - 1 - c].Icon;
                            child.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            for (int i = 0; i < playerCount; i++)
            {
                var playerTransform = _playerImages[i].transform;

                _tween = Tween.Scale(
                    playerTransform,
                    Vector3.zero,
                    Vector3.one * _playerTargetScale,
                    _playerScaleDuration,
                    _playerScaleEase
                );

                await AnimateChildren(playerTransform);

                await UniTask.Delay(TimeSpan.FromSeconds(_playerAnimDelay));
            }

            await _tween;

            await UniTask.Delay(TimeSpan.FromSeconds(_finalPauseDuration));
        }

        private async UniTask AnimateChildren(Transform parent)
        {
            int childCount = parent.childCount;
            if (childCount == 0) return;

            Vector3[] finalPositions = new Vector3[childCount];
            float angleStep = 360f / childCount;

            for (int i = 0; i < childCount; i++)
            {
                float angle = -i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                finalPositions[i] = new Vector3(
                    Mathf.Cos(rad) * _childRadius,
                    Mathf.Sin(rad) * _childRadius,
                    0f
                );
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf) continue; 

                Tween.Scale(
                    child,
                    Vector3.zero,
                    Vector3.one,
                    _childAnimDuration,
                    _augmentIconScaleEase
                );

                Tween.LocalPosition(
                    child,
                    Vector3.zero,
                    finalPositions[i],
                    _childAnimDuration,
                    _augmentIconMoveEase
                );

                await UniTask.Delay(TimeSpan.FromSeconds(_childAnimDelay));
            }
        }
    }
}
