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
        #region Variables

        #region References

        [Header("Player Image References")] [SerializeField]
        private Image[] _playerImages;

        #endregion

        #region Player Animation Settings

        [Header("Player Animation Settings")] [SerializeField]
        private float _playerScaleDuration = 0.4f;

        [SerializeField] private float _playerTargetScale = 0.65f;
        [SerializeField] private float _playerAnimDelay = 0.15f;
        [SerializeField] private Ease _playerScaleEase = Ease.OutBack;

        #endregion

        #region Child / Augment Icon Animation

        [Header("Child / Augment Icon Animation")] [SerializeField]
        private float _augmentIconRadius = 225f;

        [SerializeField] private float _childAnimDelay = 0.3f;
        [SerializeField] private float _augmentIconAnimDuration = 0.8f;
        [SerializeField] private Ease _augmentIconScaleEase = Ease.OutBack;
        [SerializeField] private Ease _augmentIconMoveEase = Ease.OutCubic;

        #endregion

        #region Timing

        [Header("Timing")] [SerializeField] private float _finalPauseDuration = 1.5f;

        #endregion

        #region Runtime State

        private Tween _tween;

        #endregion

        #endregion

        public async UniTask AnimatePlayerImagesWithAugments(List<List<SO_Augment>> playerAugments)
        {
            int playerCount = playerAugments.Count;

            for (int i = 0; i < _playerImages.Length; i++)
            {
                bool active = i < playerCount;
                var playerImage = _playerImages[i];

                playerImage.gameObject.SetActive(active);
                if (!active) continue;

                var playerTransform = playerImage.transform;
                playerTransform.localScale = Vector3.zero;

                var augments = playerAugments[i];
                int childCount = playerTransform.childCount;

                for (int c = 0; c < childCount; c++)
                {
                    var child = playerTransform.GetChild(c);
                    child.localScale = Vector3.zero;
                    child.localPosition = Vector3.zero;

                    if (c >= augments.Count)
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }

                    var augment = augments[augments.Count - 1 - c];

                    var rarityImage = child.GetComponent<Image>();
                    if (rarityImage != null)
                        rarityImage.sprite = augment.RarityIcon;

                    if (child.childCount > 0)
                    {
                        var logoImage = child.GetChild(0).GetComponent<Image>();
                        if (logoImage != null)
                            logoImage.sprite = augment.Icon;
                    }

                    child.gameObject.SetActive(true);
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

                await UniTask.Yield();
                await AnimateChildren(playerTransform);
                await UniTask.Delay(TimeSpan.FromSeconds(_playerAnimDelay));
            }

            if (_tween.isAlive)
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
                    Mathf.Cos(rad) * _augmentIconRadius,
                    Mathf.Sin(rad) * _augmentIconRadius,
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
                    _augmentIconAnimDuration,
                    _augmentIconScaleEase
                );

                Tween.LocalPosition(
                    child,
                    Vector3.zero,
                    finalPositions[i],
                    _augmentIconAnimDuration,
                    _augmentIconMoveEase
                );

                await UniTask.Delay(TimeSpan.FromSeconds(_childAnimDelay));
            }
        }
    }
}