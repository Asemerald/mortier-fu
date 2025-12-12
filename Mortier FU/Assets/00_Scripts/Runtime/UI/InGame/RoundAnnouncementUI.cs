using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [Header("Parameters")] [SerializeField]
        private float _postPlayDelay = 0.5f;

        [SerializeField] private float _panelFadeDuration = 0.35f;

        [Header("References"), SerializeField] private GameObject _roundAnnouncement;

        [SerializeField] private GameObject _readyGameObject;
        [SerializeField] private GameObject _playGameObject;
        [SerializeField] private GameObject _goldenBombshellGameObject;

        [SerializeField] private Image _countdownImage;
        [SerializeField] private List<Sprite> _countdownSprites;

        [SerializeField] private CanvasGroup _panelGroup;

        private GameModeBase _gm;

        private void Awake()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
        }

        private void OnEnable()
        {
            if (_gm == null)
            {
                Logs.LogWarning("[RoundAnnouncementUI]: Game mode not found");
                return;
            }

            _gm.OnGameStarted += OnGameStarted;
            _gm.OnRoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            if (_gm == null) return;

            _gm.OnGameStarted -= OnGameStarted;
            _gm.OnRoundStarted -= OnRoundStarted;
        }

        private void OnGameStarted()
        {
            _gm.OnGameStarted -= OnGameStarted;
            InitializeUI();
        }

        private void OnRoundStarted(int currentRound)
        {
            UpdateMatchPointIndicator();
            RunCountdown().Forget();
        }

        private void InitializeUI()
        {
            HidePanel();
            _panelGroup.alpha = 0f;
            
            _readyGameObject.SetActive(true);
            _playGameObject.SetActive(false);
            _goldenBombshellGameObject.SetActive(false);
        }

        private async UniTaskVoid RunCountdown()
        {
            ResetCountdownUI();
            ShowPanel();

            float remaining;

            AnimateCountdownImage().Forget();

            do
            {
                remaining = _gm.CountdownRemainingTime;
                UpdateCountdownVisual(Mathf.CeilToInt(remaining));
                await UniTask.Yield();
            } while (remaining > 0f);

            ShowCountdown();
            await UniTask.Delay(TimeSpan.FromSeconds(_postPlayDelay));

            await FadeOutPanel(_panelFadeDuration);
            HidePanel();
        }
        
        private void UpdateCountdownVisual(int t)
        {
            int index = t switch
            {
                <= 1 => 0,
                <= 2 => 1,
                <= 3 => 2,
                _ => 2
            };

            if (_countdownSprites == null || _countdownSprites.Count <= index) return;
            
            var newSprite = _countdownSprites[index];
            
            if (_countdownImage.sprite == newSprite) return;

            _countdownImage.sprite = newSprite;
        }

        private async UniTaskVoid AnimateCountdownImage()
        {
            var target = _countdownImage.transform;
            Vector3 targetScale = target.localScale;
            int cycles = Mathf.CeilToInt(_gm.CountdownRemainingTime);

            const float growthDuration = 0.3f;
            const float shrinkDuration = 0.3f;
            float bumpDuration = 1f - growthDuration - shrinkDuration;

            // Grow from zero, then scale down slowly while moving up and down to quickly shrink down to zero.
            var seq = Sequence.Create(cycles, Sequence.SequenceCycleMode.Restart)
                .Chain(Tween.Scale(target, Vector3.zero, targetScale, growthDuration, Ease.OutBack))
                .Group(Tween.Rotation(target, Quaternion.Euler(0f, 0f, 180), Quaternion.Euler(0f, 0f, 0f),
                    growthDuration * 0.9f, Ease.OutBack, startDelay: growthDuration * 0.1f))
                //.Chain(Tween.Scale(target, targetScale * 0.5f, bumpDuration, Ease.InQuad))
                .ChainDelay(bumpDuration)
                .Chain(Tween.Scale(target, Vector3.zero, shrinkDuration, Ease.InBack))
                .Group(Tween.Rotation(target, Quaternion.Euler(0f, 0f, 180f), shrinkDuration * 0.9f, Ease.InBack,
                    startDelay: shrinkDuration * 0.1f));

            await seq;
        }

        private void ResetCountdownUI()
        {
            _readyGameObject.SetActive(true);
            _playGameObject.SetActive(false);
            _countdownImage.enabled = true;

            _panelGroup.alpha = 1f;
        }

        private void ShowCountdown()
        {
            _readyGameObject.SetActive(false);
            _countdownImage.enabled = false;
            _playGameObject.SetActive(true);
        }

        private async UniTask FadeOutPanel(float duration)
        {
            float t = 0f;
            float startAlpha = _panelGroup.alpha;

            while (t < duration)
            {
                t += Time.deltaTime;
                _panelGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / duration);
                await UniTask.Yield();
            }

            _panelGroup.alpha = 1f;
        }

        private void UpdateMatchPointIndicator()
        {
            if (_gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;

            for (int i = 0; i < _gm.Teams.Count; i++)
            {
                if (_gm.Teams[i].Score < _gm.Data.ScoreToWin) continue;
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
        }
        
        private void ShowPanel() => _roundAnnouncement.SetActive(true);

        private void HidePanel() => _roundAnnouncement.SetActive(false);
    }
}