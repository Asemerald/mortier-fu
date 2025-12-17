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
        [SerializeField] private float _childRadius = 30f;
        [SerializeField] private float _childAnimDelay = 0.05f;
        [SerializeField] private float _childAnimDuration = 0.3f;

        private Tween _tween;

        public async UniTask AnimatePlayerImages(int playerCount)
        {
            for (int i = 0; i < _playerImages.Length; i++)
            {
                bool active = i < playerCount;
                _playerImages[i].gameObject.SetActive(active);
                if (!active) continue;

                _playerImages[i].transform.localScale = Vector3.zero;

                foreach (Transform child in _playerImages[i].transform)
                {
                    child.localScale = Vector3.zero;
                    child.localPosition = Vector3.zero;
                }
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

                AnimateChildren(_playerImages[i].transform).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
            }

            await _tween;
        }

        private async UniTaskVoid AnimateChildren(Transform parent)
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

                Tween.Scale(child, Vector3.zero, Vector3.one, _childAnimDuration, Ease.OutBack);

                Tween.LocalPosition(child, Vector3.zero, finalPositions[i], _childAnimDuration, Ease.OutCubic);

                await UniTask.Delay(TimeSpan.FromSeconds(_childAnimDelay));
            }
        }
    }
}