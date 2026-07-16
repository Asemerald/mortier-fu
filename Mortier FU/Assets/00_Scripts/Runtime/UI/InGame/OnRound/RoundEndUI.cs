using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")]
        [SerializeField] private Image _winnerTitleImage;
        [SerializeField] private Image _winnerBackgroundImage;
        [SerializeField] private Image _winnerBackgroundColorImage;

        [Header("Player Panels")]
        [SerializeField] private RectTransform[] _playerSlots;
        [SerializeField] private Image[] _playerIcons;
        [SerializeField] private Slider[] _scoreSliders;

        [Header("Player Assets")]
        [SerializeField] private Sprite[] _playerDefaultSprites;
        [SerializeField] private Sprite[] _playerWinnerIcons;
        [SerializeField] private Sprite[] _winnerTitleSprites;
        [SerializeField] private Sprite[] _winnerBackgrounds;
        [SerializeField] private Sprite[] _winnerBackgroundColors;

        [Header("Leaderboard")]
        [SerializeField] private Vector2[] _leaderboardPositions;
        [SerializeField] private Sprite[] _placeSprites;
        [SerializeField] private Image[] _placeImages;

        [Header("Score Text")]
        [SerializeField] private TextMeshProUGUI[] _scoreTexts;

        [Header("Kill Context")]
        [SerializeField] private Image[] _killContextImages;
        [SerializeField] private Sprite _bombshellKillContextSprite;
        [SerializeField] private Sprite _fallKillContextSprite;
        [SerializeField] private Sprite _vehicleCrashKillContextSprite;

        [Header("Golden Bombshell")]
        [SerializeField] private Sprite[] _goldenBombshellSprites;
        [SerializeField] private Image[] _goldenBombshellImg;
        [SerializeField] private Image[] _goldenBombshellBgdImg;
        [SerializeField] private Image[] _goldenBombshellHaloImg;

        [Header("Timing")]
        [SerializeField] private float _sliderAnimationDuration = 0.13f;
        [SerializeField] private float _reorderPlayerDelay = 0.13f;
        [SerializeField] private float _hideDelay = 0.3f;
        [SerializeField] private float _updateSlidersDelay = 0.08f;
        [SerializeField] private float _startKillAnimDelay = 0.05f;

        [Header("Leaderboard Animation")]
        [SerializeField] private float _leaderboardMoveDuration = 0.35f;
        [SerializeField] private float _topPlayerScaleDuration = 0.2f;
        [SerializeField] private float _topPlayerScaleFactor = 1.05f;

        [Header("Popup Animation")]
        [SerializeField] private float _showPlacementScaleDuration = 0.12f;
        [SerializeField] private float _hidePlacementScaleDuration = 0.12f;
        [SerializeField] private float _showKillScaleDuration = 0.12f;
        [SerializeField] private float _hideKillScaleDuration = 0.12f;

        [Header("Golden Bombshell Animation")]
        [SerializeField] private float _goldenBombshellScaleUpDuration = 0.5f;
        [SerializeField] private float _goldenBombshellScaleUpFactor = 1.15f;

        [Header("Eases")]
        [SerializeField] private Ease _leaderboardTweenEase = Ease.OutBack;
        [SerializeField] private Ease _scaleTweenEase = Ease.OutBack;
        [SerializeField] private Ease _showPlacementEase = Ease.OutBack;
        [SerializeField] private Ease _hidePlacementEase = Ease.InBack;
        [SerializeField] private Ease _showKillEase = Ease.OutBack;
        [SerializeField] private Ease _hideKillEase = Ease.InBack;
        [SerializeField] private Ease _goldenBombshellScaleUp = Ease.OutBack;
        [SerializeField] private Ease _goldenBombshellScaleDown = Ease.InBack;
        
        [Header("Fx")]
        [SerializeField] private RectTransform _fxPrefabWinner;

        private GameModeBase _gm;
        private CancellationTokenSource _lifetimeCancellation;
        private CancellationTokenSource _goldenBombshellCts;

        private int[] _leaderboardOrder;
        private Vector3 _originalScale;
        private int _previousTopPlayerIndex;

        private readonly HashSet<int> _winnerIconPlayerIndexes = new();
        private readonly HashSet<int> _goldenBombshellPlayerIndexes = new();
        
        private void Awake()
        {
            _originalScale = _playerSlots[0].transform.localScale;
            _leaderboardOrder = Enumerable.Range(0, _playerSlots.Length).ToArray();

            ResetUI();
            SetPlayersToLeaderboardOrder(_leaderboardOrder);
        }

        private void OnEnable()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = new CancellationTokenSource();

            SubscribeGameMode();
        }

        private void OnDisable()
        {
            UnsubscribeGameMode();
            StopRuntimeAnimations();

            _lifetimeCancellation?.Cancel();

            ResetUI();
        }

        private void OnDestroy()
        {
            UnsubscribeGameMode();
            StopRuntimeAnimations();

            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        private void SubscribeGameMode()
        {
            UnsubscribeGameMode();

            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
                return;

            _gm.OnRoundEndedAsync += AnimateRoundEndSequence;
            _gm.OnScoreDisplayOver += ResetUI;

            InitializeSliders();
        }

        private void UnsubscribeGameMode()
        {
            if (_gm == null)
                return;

            _gm.OnRoundEndedAsync -= AnimateRoundEndSequence;
            _gm.OnScoreDisplayOver -= ResetUI;
            _gm = null;
        }

        private void InitializeSliders()
        {
            if (_gm == null)
                return;

            for (int i = 0; i < _scoreSliders.Length; i++)
            {
                if (!_scoreSliders[i])
                    continue;

                _scoreSliders[i].maxValue = _gm.ScoreToWin;
                _scoreSliders[i].value = Mathf.Clamp(_scoreSliders[i].value, 0f, _gm.ScoreToWin);
            }
        }

        private void StopRuntimeAnimations()
        {
            _goldenBombshellCts?.Cancel();
            _goldenBombshellCts?.Dispose();
            _goldenBombshellCts = null;
        }

        private async UniTask AnimateRoundEndSequence(RoundInfo round, CancellationToken cancellationToken)
        {
            using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);

            CancellationToken ct = linkedCancellation.Token;

            ResetUI();

            InitializePlayerPanels(_leaderboardOrder);
            ShowRoundWinner(round.WinningTeam);

            await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);

            await AnimatePlacementRewards(ct);
            await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay), cancellationToken: ct);

            var sortedTeams = _gm.GetPlayerTeamsWinnersOrder();

            await AnimateLeaderboardPositions(sortedTeams, ct);

            _leaderboardOrder = sortedTeams.Select(team => team.Index).ToArray();

            await RevealFinalScoreState(ct);

            await UniTask.Delay(TimeSpan.FromSeconds(GetScoreboardMinimumDuration()), cancellationToken: ct);

            _goldenBombshellCts?.Cancel();
            
            if (_gm != null && _gm.IsGameOver(out PlayerTeam playerTeam))
                ResetUI();
        }

        private async UniTask AnimatePlacementRewards(CancellationToken ct)
        {
            await ShowPlacementPopups(ct);
            await ShowPlacementScoreTexts(ct);

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay), cancellationToken: ct);

            await HidePlacementPopups(ct);

            await UniTask.Delay(TimeSpan.FromSeconds(_updateSlidersDelay), cancellationToken: ct);

            await AnimatePlacementSliders(ct);

            await UniTask.Delay(TimeSpan.FromSeconds(_startKillAnimDelay), cancellationToken: ct);

            await AnimateKillsByRound(ct);
        }

        private async UniTask ShowPlacementPopups(CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;
                int rankIndex = team.Rank - 1;

                if (!IsValidPlayerIndex(idx) || rankIndex < 0 || rankIndex >= _placeSprites.Length)
                    continue;

                Image placeImage = _placeImages[idx];

                placeImage.sprite = _placeSprites[rankIndex];
                placeImage.SetNativeSize();
                placeImage.transform.localScale = Vector3.zero;
                placeImage.gameObject.SetActive(true);

                tasks.Add(Tween.Scale(placeImage.transform, Vector3.one, _showPlacementScaleDuration, _showPlacementEase).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask ShowPlacementScoreTexts(CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                ScoreRewardData reward = GetPlacementReward(team);

                if (!reward.ShouldDisplay())
                    continue;

                TextMeshProUGUI scoreText = _scoreTexts[idx];

                scoreText.text = reward.GetDisplayText();
                scoreText.transform.localScale = Vector3.zero;
                scoreText.gameObject.SetActive(true);

                tasks.Add(Tween.Scale(scoreText.transform, Vector3.one, _showPlacementScaleDuration, _showPlacementEase).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask HidePlacementPopups(CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                Image placeImage = _placeImages[idx];
                TextMeshProUGUI scoreText = _scoreTexts[idx];

                if (scoreText.gameObject.activeSelf)
                {
                    tasks.Add(Tween.Scale(placeImage.transform, Vector3.zero, _hidePlacementScaleDuration, _hidePlacementEase)
                        .Group(Tween.Scale(scoreText.transform, Vector3.zero, _hidePlacementScaleDuration, _hidePlacementEase)).ToUniTask(cancellationToken: ct));
                }
                else
                {
                    tasks.Add(Tween.Scale(placeImage.transform, Vector3.zero, _hidePlacementScaleDuration, _hidePlacementEase).ToUniTask(cancellationToken: ct));
                }
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            HideAllScoreTexts();
        }

        private async UniTask AnimatePlacementSliders(CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                int bonus = GetPlacementReward(team).Score;

                if (bonus <= 0)
                    continue;

                Slider slider = _scoreSliders[idx];

                int start = Mathf.RoundToInt(slider.value);
                int max = Mathf.RoundToInt(slider.maxValue);

                if (start >= max)
                    continue;

                int end = Mathf.Min(start + bonus, max);

                tasks.Add(AnimateSlider(slider, start, end, _sliderAnimationDuration, ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateKillsByRound(CancellationToken ct)
        {
            int maxKills = _gm.Teams.Max(team => GetTeamRoundKills(team).Count);

            for (int killRound = 0; killRound < maxKills; killRound++)
            {
                await ShowKillContextPopups(killRound, ct);
                await ShowKillScoreTexts(killRound, ct);

                await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay), cancellationToken: ct);

                await HideKillPopups(ct);

                await UniTask.Delay(TimeSpan.FromSeconds(_updateSlidersDelay), cancellationToken: ct);

                await AnimateKillSliders(killRound, ct);

                await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
            }
        }

        private async UniTask ShowKillContextPopups(int killRound, CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;
                var kills = GetTeamRoundKills(team);

                if (!IsValidPlayerIndex(idx) || killRound >= kills.Count)
                    continue;

                Image contextImage = _killContextImages[idx];

                contextImage.sprite = GetKillContextSprite(kills[killRound]);
                contextImage.SetNativeSize();
                contextImage.transform.localScale = Vector3.zero;
                contextImage.gameObject.SetActive(true);

                tasks.Add(Tween.Scale(contextImage.transform, Vector3.one, _showKillScaleDuration, _showKillEase).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask ShowKillScoreTexts(int killRound, CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;
                var kills = GetTeamRoundKills(team);

                if (!IsValidPlayerIndex(idx) || killRound >= kills.Count)
                    continue;

                ScoreRewardData reward = GetKillReward(kills[killRound]);

                if (!reward.ShouldDisplay())
                {
                    continue;
                }
                    

                TextMeshProUGUI scoreText = _scoreTexts[idx];

                scoreText.text = reward.GetDisplayText();
                scoreText.transform.localScale = Vector3.zero;
                scoreText.gameObject.SetActive(true);

                tasks.Add(Tween.Scale(scoreText.transform, Vector3.one, _showKillScaleDuration, _showKillEase).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask HideKillPopups(CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                Image contextImage = _killContextImages[idx];
                TextMeshProUGUI scoreText = _scoreTexts[idx];

                if (!contextImage.gameObject.activeSelf)
                    continue;

                if (scoreText.gameObject.activeSelf)
                {
                    tasks.Add(Tween.Scale(contextImage.transform, Vector3.zero, _hideKillScaleDuration, _hideKillEase)
                        .Group(Tween.Scale(scoreText.transform, Vector3.zero, _hideKillScaleDuration, _hideKillEase)).ToUniTask(cancellationToken: ct));
                }
                else
                {
                    tasks.Add(Tween.Scale(contextImage.transform, Vector3.zero, _hideKillScaleDuration, _hideKillEase).ToUniTask(cancellationToken: ct));
                }
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                _killContextImages[idx].gameObject.SetActive(false);
                HideScoreText(idx);
            }
        }

        private async UniTask AnimateKillSliders(int killRound, CancellationToken ct)
        {
            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;
                var kills = GetTeamRoundKills(team);

                if (!IsValidPlayerIndex(idx) || killRound >= kills.Count)
                    continue;

                int gain = GetKillReward(kills[killRound]).Score;

                if (gain <= 0)
                    continue;

                Slider slider = _scoreSliders[idx];

                int start = Mathf.RoundToInt(slider.value);
                int max = Mathf.RoundToInt(slider.maxValue);

                if (start >= max)
                    continue;

                int end = Mathf.Min(start + gain, max);

                tasks.Add(AnimateSlider(slider, start, end, _sliderAnimationDuration, ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateSlider(Slider slider, float start, float end, float duration, CancellationToken ct)
        {
            if (!slider)
                return;

            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_ScoreIncrease);

            slider.value = start;
            duration = Mathf.Max(0.01f, duration);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                slider.value = Mathf.Lerp(start, end, elapsed / duration);

                await UniTask.Yield();

                ct.ThrowIfCancellationRequested();
            }

            slider.value = end;
        }

        private async UniTask AnimateLeaderboardPositions(List<PlayerTeam> sortedTeams, CancellationToken ct)
        {
            if (sortedTeams == null || sortedTeams.Count == 0)
                return;

            int currentTopIdx = sortedTeams[0].Index;
            bool isSameTopPlayer = currentTopIdx == _previousTopPlayerIndex;

            if (!isSameTopPlayer && IsValidPlayerIndex(currentTopIdx))
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_NewLeader);

                await Tween.Scale(_playerSlots[currentTopIdx], _originalScale * _topPlayerScaleFactor, _topPlayerScaleDuration, _scaleTweenEase).ToUniTask(cancellationToken: ct);
            }

            var tasks = new List<UniTask>();

            for (int rank = 0; rank < sortedTeams.Count; rank++)
            {
                int idx = sortedTeams[rank].Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                tasks.Add(Tween.UIAnchoredPosition(_playerSlots[idx], _leaderboardPositions[rank], _leaderboardMoveDuration, _leaderboardTweenEase).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            if (!isSameTopPlayer && IsValidPlayerIndex(currentTopIdx))
                await Tween.Scale(_playerSlots[currentTopIdx], _originalScale, _topPlayerScaleDuration, _scaleTweenEase).ToUniTask(cancellationToken: ct);

            _previousTopPlayerIndex = currentTopIdx;
        }

        private async UniTask RevealFinalScoreState(CancellationToken ct)
        {
            _gm.ScoreController.UpdatePlayerVisualsAfterRound(_gm.Teams);
            await UniTask.WhenAll(RevealWinnerIcons(), ShowGoldenBombshellIndicator(ct));
        }

        private UniTask RevealWinnerIcons()
        {
            List<PlayerTeam> topTeams = GetTopScoreTeams();
            var newWinnerIndexes = new HashSet<int>();

            foreach (PlayerTeam team in topTeams)
            {
                int idx = team.Index;

                if (IsValidPlayerIndex(idx))
                    newWinnerIndexes.Add(idx);
            }

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                bool isWinner = newWinnerIndexes.Contains(idx);

                _playerIcons[idx].sprite = isWinner
                    ? _playerWinnerIcons[idx]
                    : _playerDefaultSprites[idx];

                SpawnFxWinner( _playerIcons[idx].transform, isWinner);
            }

            _winnerIconPlayerIndexes.Clear();

            foreach (int idx in newWinnerIndexes)
            {
                _winnerIconPlayerIndexes.Add(idx);
            }

            return UniTask.CompletedTask;
        }

        //TODO FIX HARD CODED 10F 
        private void SpawnFxWinner(Transform winnerTransform,bool isWinner)
        {
            if (!isWinner || !_fxPrefabWinner) return;
            
            RectTransform fx = Instantiate(_fxPrefabWinner,  Vector3.zero, Quaternion.identity, winnerTransform);
            fx.SetLocalPositionAndRotation(Vector2.zero, Quaternion.identity);
            Destroy(fx.gameObject,10f);
        }

        private async UniTask ShowGoldenBombshellIndicator(CancellationToken cancellationToken)
        {
            if (_gm == null)
                return;

            _goldenBombshellCts?.Cancel();
            _goldenBombshellCts?.Dispose();

            _goldenBombshellCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ct = _goldenBombshellCts.Token;

            var tasks = new List<UniTask>();

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                _goldenBombshellBgdImg[idx].gameObject.SetActive(true);

                if (!IsPlayerDisplayedAtMatchPoint(idx))
                    continue;

                bool wasAlreadyGoldenBombshell = _goldenBombshellPlayerIndexes.Contains(idx);
                _goldenBombshellPlayerIndexes.Add(idx);

                if (wasAlreadyGoldenBombshell)
                {
                    _goldenBombshellImg[idx].sprite = _goldenBombshellSprites[idx];
                    _goldenBombshellImg[idx].SetNativeSize();

                    _goldenBombshellImg[idx].transform.localScale = Vector3.one;
                    _goldenBombshellHaloImg[idx].transform.localScale = Vector3.one;

                    _goldenBombshellImg[idx].gameObject.SetActive(true);
                    _goldenBombshellHaloImg[idx].gameObject.SetActive(true);

                    continue;
                }

                Image bombshell = _goldenBombshellImg[idx];
                Image halo = _goldenBombshellHaloImg[idx];

                bombshell.sprite = _goldenBombshellSprites[idx];
                bombshell.SetNativeSize();

                bombshell.transform.localScale = Vector3.zero;
                halo.transform.localScale = Vector3.zero;

                bombshell.gameObject.SetActive(true);
                halo.gameObject.SetActive(true);

                tasks.Add(Tween.Scale(bombshell.transform, Vector3.one, _goldenBombshellScaleUpDuration, _goldenBombshellScaleUp)
                        .Group(Tween.Scale(halo.transform, Vector3.one, _goldenBombshellScaleUpDuration, _goldenBombshellScaleUp)).ToUniTask(cancellationToken: ct));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            StartGoldenBombshellLoops(ct);
        }

        private void StartGoldenBombshellLoops(CancellationToken ct)
        {
            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                if (_goldenBombshellImg[idx].gameObject.activeInHierarchy)
                    AnimateGoldenBombshellLoop(_goldenBombshellImg[idx].transform, ct).Forget();

                if (_goldenBombshellHaloImg[idx].gameObject.activeInHierarchy)
                    AnimateHaloLoop(_goldenBombshellHaloImg[idx].transform, ct).Forget();
            }
        }

        private async UniTask AnimateGoldenBombshellLoop(Transform target, CancellationToken token)
        {
            Vector3 baseScale = Vector3.one;
            Vector3 upScale = Vector3.one * _goldenBombshellScaleUpFactor;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Tween.PunchScale(target, Vector3.one * 0.08f, 0.25f).ToUniTask(cancellationToken: token);
                    await Tween.Scale(target, upScale, 0.2f, _goldenBombshellScaleUp).ToUniTask(cancellationToken: token);
                    await Tween.Scale(target, baseScale, 0.2f, _goldenBombshellScaleDown).ToUniTask(cancellationToken: token);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                target.localScale = baseScale;
            }
        }

        private async UniTask AnimateHaloLoop(Transform target, CancellationToken token)
        {
            Vector3 baseScale = Vector3.one;
            Vector3 upScale = Vector3.one * 1.08f;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Tween.Rotation(target, new Vector3(0f, 0f, -90f), 6f, Ease.Linear).ToUniTask(cancellationToken: token);
                    await Tween.Scale(target, upScale, 1.2f, Ease.OutSine).ToUniTask(cancellationToken: token);
                    await Tween.Scale(target, baseScale, 1.2f, Ease.InSine).ToUniTask(cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                target.localScale = baseScale;
            }
        }

        private void InitializePlayerPanels(int[] orderOverride = null)
        {
            for (int i = 0; i < _playerSlots.Length; i++)
            {
                _playerSlots[i].gameObject.SetActive(false);
                _goldenBombshellBgdImg[i].gameObject.SetActive(false);
            }

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                _playerSlots[idx].gameObject.SetActive(true);
                _playerSlots[idx].transform.localScale = _originalScale;

                _playerIcons[idx].sprite = _winnerIconPlayerIndexes.Contains(idx) ? _playerWinnerIcons[idx] : _playerDefaultSprites[idx];

                _playerIcons[idx].gameObject.SetActive(true);

                _goldenBombshellBgdImg[idx].gameObject.SetActive(true);

                if (!_goldenBombshellPlayerIndexes.Contains(idx)) continue;
                
                _goldenBombshellImg[idx].sprite = _goldenBombshellSprites[idx];
                _goldenBombshellImg[idx].SetNativeSize();
                _goldenBombshellImg[idx].transform.localScale = Vector3.one;
                _goldenBombshellImg[idx].gameObject.SetActive(true);

                _goldenBombshellHaloImg[idx].transform.localScale = Vector3.one;
                _goldenBombshellHaloImg[idx].gameObject.SetActive(true);
            }

            if (orderOverride != null)
                SetPlayersToLeaderboardOrder(orderOverride);
        }

        private void ShowRoundWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null || !IsValidPlayerIndex(winningTeam.Index))
                return;

            int idx = winningTeam.Index;

            _winnerTitleImage.sprite = _winnerTitleSprites[idx];
            _winnerBackgroundImage.sprite = _winnerBackgrounds[idx];
            _winnerBackgroundColorImage.sprite = _winnerBackgroundColors[idx];

            _winnerTitleImage.gameObject.SetActive(true);
            _winnerBackgroundImage.gameObject.SetActive(true);
            _winnerBackgroundColorImage.gameObject.SetActive(true);
        }

        private void SetPlayersToLeaderboardOrder(int[] order)
        {
            for (int rank = 0; rank < order.Length; rank++)
            {
                int idx = order[rank];

                if (!IsValidPlayerIndex(idx))
                    continue;

                _playerSlots[idx].anchoredPosition = _leaderboardPositions[rank];
            }
        }

        private ScoreRewardData GetPlacementReward(PlayerTeam team)
        {
            if (_gm == null || _gm.Data == null || team == null)
                return ScoreRewardData.Zero;

            return _gm.Data.GetPlacementReward(_gm.Teams.Count, team.Rank);
        }

        private ScoreRewardData GetKillReward(E_DeathCause cause)
        {
            if (_gm == null || _gm.Data == null)
                return ScoreRewardData.Zero;

            return _gm.Data.GetKillReward(cause);
        }

        private Sprite GetKillContextSprite(E_DeathCause cause)
        {
            return cause switch
            {
                E_DeathCause.FallAfterExplosion =>_bombshellKillContextSprite,
                E_DeathCause.BombshellExplosion => _bombshellKillContextSprite,
                E_DeathCause.Fall => _fallKillContextSprite,
                E_DeathCause.VehicleCrash => _vehicleCrashKillContextSprite,
                _ => null
            };
        }

        private List<E_DeathCause> GetTeamRoundKills(PlayerTeam team)
        {
            return team.Members
                .Where(member => member != null && member.Metrics.RoundKills != null)
                .SelectMany(member => member.Metrics.RoundKills)
                .ToList();
        }

        private List<PlayerTeam> GetTopScoreTeams()
        {
            var topTeams = new List<PlayerTeam>();

            int maxDisplayedScore = int.MinValue;

            foreach (PlayerTeam team in _gm.Teams)
            {
                int idx = team.Index;

                if (!IsValidPlayerIndex(idx))
                    continue;

                int displayedScore = Mathf.RoundToInt(_scoreSliders[idx].value);

                if (displayedScore > maxDisplayedScore)
                {
                    topTeams.Clear();
                    topTeams.Add(team);
                    maxDisplayedScore = displayedScore;
                }
                else if (displayedScore == maxDisplayedScore)
                {
                    topTeams.Add(team);
                }
            }

            return topTeams;
        }

        private bool IsPlayerDisplayedAtMatchPoint(int playerIndex) => IsValidPlayerIndex(playerIndex) && _scoreSliders[playerIndex].value >= _gm.ScoreToWin - 0.01f;

        private bool IsValidPlayerIndex(int idx)
        {
            return idx >= 0 &&
                   idx < _playerSlots.Length &&
                   idx < _playerIcons.Length &&
                   idx < _scoreSliders.Length &&
                   idx < _scoreTexts.Length &&
                   idx < _placeImages.Length &&
                   idx < _killContextImages.Length &&
                   idx < _goldenBombshellImg.Length &&
                   idx < _goldenBombshellBgdImg.Length &&
                   idx < _goldenBombshellHaloImg.Length;
        }

        private float GetScoreboardMinimumDuration()
        {
            if (_gm != null && _gm.FlowSettings)
                return Mathf.Max(0f, _gm.FlowSettings.ScoreboardMinimumDuration);

            return _gm != null ? Mathf.Max(0f, _gm.Data.StopShowScoreBoardDelay) : 5f;
        }

        private void HideScoreText(int index)
        {
            if (!IsValidPlayerIndex(index))
                return;

            _scoreTexts[index].gameObject.SetActive(false);
            _scoreTexts[index].text = string.Empty;
            _scoreTexts[index].transform.localScale = Vector3.zero;
        }

        private void HideAllScoreTexts()
        {
            for (int i = 0; i < _scoreTexts.Length; i++)
            {
                _scoreTexts[i].gameObject.SetActive(false);
                _scoreTexts[i].text = string.Empty;
                _scoreTexts[i].transform.localScale = Vector3.zero;
            }
        }

        private void ResetUI()
        {
            _goldenBombshellCts?.Cancel();

            _winnerTitleImage.gameObject.SetActive(false);
            _winnerBackgroundImage.gameObject.SetActive(false);
            _winnerBackgroundColorImage.gameObject.SetActive(false);

            for (int i = 0; i < _playerSlots.Length; i++)
            {
                _playerSlots[i].gameObject.SetActive(false);
                _placeImages[i].gameObject.SetActive(false);
                _killContextImages[i].gameObject.SetActive(false);

                _playerIcons[i].sprite = _playerDefaultSprites[i];

                HideScoreText(i);
            }

            for (int i = 0; i < _goldenBombshellImg.Length; i++)
            {
                _goldenBombshellImg[i].transform.localScale = Vector3.zero;
                _goldenBombshellImg[i].gameObject.SetActive(false);

                _goldenBombshellHaloImg[i].transform.localScale = Vector3.zero;
                _goldenBombshellHaloImg[i].transform.rotation = Quaternion.identity;
                _goldenBombshellHaloImg[i].gameObject.SetActive(false);

                _goldenBombshellBgdImg[i].transform.localScale = Vector3.one;
                _goldenBombshellBgdImg[i].gameObject.SetActive(false);
            }
        }
    }
}