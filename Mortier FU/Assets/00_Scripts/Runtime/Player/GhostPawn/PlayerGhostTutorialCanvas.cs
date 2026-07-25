using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public sealed class PlayerGhostTutorialCanvas : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _animatedRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Input Visuals")]
        [SerializeField] private GameObject[] _keyboardObjects;
        [SerializeField] private GameObject[] _gamepadObjects;

        [Header("Player Icon")]
        [SerializeField] private Image _playerIconImage;

        [Tooltip("Index 0 = Blue, 1 = Red, 2 = Green, 3 = Yellow.")]
        [SerializeField] private Sprite[] _playerIconsByIndex = new Sprite[4];

        [Header("Show Animation")]
        [SerializeField, Min(0.01f)] private float _showDuration = 0.35f;
        [SerializeField] private Ease _showEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float _startScale = 0f;
        [SerializeField, Min(0f)] private float _targetScale = 1f;

        [Header("Display")]
        [SerializeField, Min(0f)] private float _visibleDuration = 4f;

        [Header("Hide Animation")]
        [SerializeField, Min(0.01f)] private float _hideDuration = 0.25f;
        [SerializeField] private Ease _hideEase = Ease.InBack;
        [SerializeField, Min(0f)] private float _hideScale = 0f;

        private CancellationTokenSource _cancellation;
        private Tween _scaleTween;
        private Tween _alphaTween;

        private void Awake()
        {
            ResolveReferences();
            HideInstant();
        }

        private void OnDisable()
        {
            CancelAnimation();
            HideInstant();
        }

        private void OnDestroy() => CancelAnimation();

        public void TryShow(PlayerManager owner)
        {
            if (!owner)
                return;

            int playerIndex = owner.PlayerIndex;

            if (!GhostTutorialSession.TryMarkShown(playerIndex))
                return;

            bool isKeyboard = owner.IsKeyboardAndMouseControlScheme();

            CancelAnimation();

            _cancellation = new CancellationTokenSource();
            PlayAsync(playerIndex, isKeyboard, _cancellation.Token).Forget();
        }

        public void HideInstant()
        {
            StopTweens();

            if (_canvasGroup)
                _canvasGroup.alpha = 0f;

            if (_animatedRoot)
                _animatedRoot.localScale = Vector3.one * _targetScale;

            if (_root)
                _root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private async UniTaskVoid PlayAsync(int playerIndex, bool isKeyboard, CancellationToken cancellationToken)
        {
            try
            {
                ApplyVisuals(playerIndex, isKeyboard);

                GameObject root = _root ? _root : gameObject;
                root.SetActive(true);

                if (_canvasGroup)
                    _canvasGroup.alpha = 0f;

                if (_animatedRoot)
                    _animatedRoot.localScale = Vector3.one * _startScale;

                await ShowAsync(cancellationToken);

                if (_visibleDuration > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(_visibleDuration), cancellationToken: cancellationToken);

                await HideAsync(cancellationToken);

                root.SetActive(false);
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                Logs.LogError($"[PlayerGhostTutorialCanvas] Failed to play ghost tutorial: {e.Message}", this);
                HideInstant();
            }
        }

        private async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            StopTweens();

            UniTask scaleTask = UniTask.CompletedTask;
            UniTask alphaTask = UniTask.CompletedTask;

            if (_animatedRoot)
            {
                _scaleTween = Tween.Scale(_animatedRoot, Vector3.one * _startScale, Vector3.one * _targetScale, _showDuration, _showEase);

                scaleTask = _scaleTween.ToUniTask(cancellationToken: cancellationToken);
            }

            if (_canvasGroup)
            {
                _alphaTween = Tween.Alpha(_canvasGroup, 0f, 1f, _showDuration, Ease.OutQuad);
                alphaTask = _alphaTween.ToUniTask(cancellationToken: cancellationToken);
            }

            await UniTask.WhenAll(scaleTask, alphaTask);
        }

        private async UniTask HideAsync(CancellationToken cancellationToken)
        {
            StopTweens();

            UniTask scaleTask = UniTask.CompletedTask;
            UniTask alphaTask = UniTask.CompletedTask;

            if (_animatedRoot)
            {
                _scaleTween = Tween.Scale(_animatedRoot, _animatedRoot.localScale, Vector3.one * _hideScale, _hideDuration, _hideEase);

                scaleTask = _scaleTween.ToUniTask(cancellationToken: cancellationToken);
            }

            if (_canvasGroup)
            {
                _alphaTween = Tween.Alpha(_canvasGroup, _canvasGroup.alpha, 0f, _hideDuration, Ease.InQuad);
                alphaTask = _alphaTween.ToUniTask(cancellationToken: cancellationToken);
            }

            await UniTask.WhenAll(scaleTask, alphaTask);
        }

        private void ApplyVisuals(int playerIndex, bool isKeyboard)
        {
            SetObjectsActive(_keyboardObjects, isKeyboard);
            SetObjectsActive(_gamepadObjects, !isKeyboard);

            if (!_playerIconImage)
                return;

            if (_playerIconsByIndex == null || playerIndex < 0 || playerIndex >= _playerIconsByIndex.Length)
            {
                Logs.LogWarning($"[PlayerGhostTutorialCanvas] No player icon configured for player index {playerIndex}.", this);
                return;
            }

            _playerIconImage.sprite = _playerIconsByIndex[playerIndex];
            _playerIconImage.enabled = _playerIconImage.sprite;
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
                return;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i])
                    objects[i].SetActive(active);
            }
        }

        private void ResolveReferences()
        {
            if (!_root)
                _root = gameObject;

            if (!_animatedRoot)
                _animatedRoot = transform as RectTransform;

            if (!_canvasGroup)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (!_canvasGroup)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void CancelAnimation()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;

            StopTweens();
        }

        private void StopTweens()
        {
            _scaleTween.Stop();
            _alphaTween.Stop();
        }
    }
}