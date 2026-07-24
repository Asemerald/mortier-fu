using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class UIConfirmationPanel : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Transform _panelRoot;

        [Header("Background")]
        [SerializeField] private Graphic _blackPanel;
        [SerializeField, Range(0f, 1f)] private float _blackPanelTargetAlpha = 0.65f;

        [Header("Text")]
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _confirmText;
        [SerializeField] private TMP_Text _cancelText;

        [Header("Open Animation")]
        [SerializeField, Min(0f)] private float _openScaleDuration = 0.25f;
        [SerializeField, Min(0f)] private float _openFadeDuration = 0.18f;
        [SerializeField] private Ease _openScaleEase = Ease.OutBack;
        [SerializeField] private Ease _openFadeEase = Ease.OutQuad;

        [Header("Close Animation")]
        [SerializeField, Min(0f)] private float _closeScaleDuration = 0.16f;
        [SerializeField, Min(0f)] private float _closeFadeDuration = 0.12f;
        [SerializeField] private Ease _closeScaleEase = Ease.InBack;
        [SerializeField] private Ease _closeFadeEase = Ease.InQuad;

        private Tween _scaleTween;
        private Tween _fadeTween;

        private bool _isOpen;

        private void Awake()
        {
            if (!_root)
                _root = gameObject;

            if (!_panelRoot)
                _panelRoot = transform;

            HideInstant();
        }

        private void OnDisable() => StopTweens();

        private void OnDestroy() => StopTweens();

        public void Configure(string description, string confirmLabel, string cancelLabel)
        {
            if (_descriptionText)
                _descriptionText.text = description;

            if (_confirmText)
                _confirmText.text = confirmLabel;

            if (_cancelText)
                _cancelText.text = cancelLabel;
        }

        public async UniTask OpenAsync(CancellationToken cancellationToken)
        {
            StopTweens();

            _isOpen = true;

            if (_root)
                _root.SetActive(true);

            if (_panelRoot)
                _panelRoot.localScale = Vector3.zero;

            SetBlackPanelAlpha(0f);

            if (_blackPanel)
                _fadeTween = Tween.Alpha(_blackPanel, _blackPanelTargetAlpha, _openFadeDuration, _openFadeEase, useUnscaledTime: true);

            if (_panelRoot)
            {
                _scaleTween = Tween.Scale(_panelRoot, Vector3.one, _openScaleDuration, _openScaleEase, useUnscaledTime: true);

                await WaitForTweenAsync(_scaleTween, cancellationToken);
            }
        }

        public async UniTask CloseAsync(CancellationToken cancellationToken)
        {
            if (!_isOpen)
                return;

            StopTweens();

            if (_blackPanel)
                _fadeTween = Tween.Alpha(_blackPanel, 0f, _closeFadeDuration, _closeFadeEase, useUnscaledTime: true);

            if (_panelRoot)
            {
                _scaleTween = Tween.Scale(_panelRoot, Vector3.zero, _closeScaleDuration, _closeScaleEase, useUnscaledTime: true);

                await WaitForTweenAsync(_scaleTween, cancellationToken);
            }

            HideInstant();
        }

        public void HideInstant()
        {
            StopTweens();

            _isOpen = false;

            SetBlackPanelAlpha(0f);

            if (_panelRoot)
                _panelRoot.localScale = Vector3.zero;

            if (_root)
                _root.SetActive(false);
        }

        private void StopTweens()
        {
            if (_scaleTween.isAlive)
                _scaleTween.Stop();

            if (_fadeTween.isAlive)
                _fadeTween.Stop();
        }

        private void SetBlackPanelAlpha(float alpha)
        {
            if (!_blackPanel)
                return;

            Color color = _blackPanel.color;
            color.a = alpha;
            _blackPanel.color = color;
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