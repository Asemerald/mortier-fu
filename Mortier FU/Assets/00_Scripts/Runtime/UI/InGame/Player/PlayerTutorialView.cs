using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class PlayerTutorialView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _container;
        [SerializeField] private Image _inputImage;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float _showDuration = 0.25f;
        [SerializeField] private Ease _showEase = Ease.OutBack;

        private Tween _scaleTween;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (!_container)
                _container = transform;

            HideInstant();
        }

        private void OnDisable() => StopTween();

        private void OnDestroy() => StopTween();

        public async UniTask ShowStepAsync(SO_Tutorial step, bool isKeyboard, CancellationToken cancellationToken)
        {
            StopTween();

            ApplyStep(step, isKeyboard);

            if (!_container)
            {
                Logs.LogWarning("[PlayerTutorialView] Container reference is missing.", this);
                return;
            }

            IsVisible = true;

            _container.gameObject.SetActive(true);
            _container.localScale = Vector3.zero;

            _scaleTween = Tween.Scale(_container, Vector3.one, _showDuration, _showEase, useUnscaledTime: true);

            await WaitForTweenAsync(_scaleTween, cancellationToken);
        }

        public void ApplyStep(SO_Tutorial step, bool isKeyboard)
        {
            if (!step)
            {
                Logs.LogWarning("[PlayerTutorialView] Cannot apply null tutorial step.", this);
                ClearVisuals();
                return;
            }

            Sprite sprite = step.GetSpriteByInput(isKeyboard);

            if (_inputImage)
            {
                _inputImage.sprite = sprite;
                _inputImage.enabled = sprite;

                if (sprite)
                    _inputImage.rectTransform.sizeDelta = step.GetSizeByInput(isKeyboard);
                else
                    Logs.LogWarning($"[PlayerTutorialView] Tutorial step '{step.name}' has no sprite for current input.", this);
            }

            if (_descriptionText)
                _descriptionText.text = step.ExplanationText;
        }

        public void HideInstant()
        {
            StopTween();

            IsVisible = false;

            if (_container)
            {
                _container.localScale = Vector3.zero;
                _container.gameObject.SetActive(false);
            }

            ClearVisuals();
        }

        private void ClearVisuals()
        {
            if (_inputImage)
            {
                _inputImage.sprite = null;
                _inputImage.enabled = false;
            }

            if (_descriptionText)
                _descriptionText.text = string.Empty;
        }

        private void StopTween()
        {
            if (_scaleTween.isAlive)
                _scaleTween.Stop();
        }

        private static async UniTask WaitForTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            while (tween.isAlive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
    }
}