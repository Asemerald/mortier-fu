using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")] [SerializeField] private Image _winnerTitleImage;
        [SerializeField] private Image _winnerBackgroundImage;
        [SerializeField] private Image _winnerBackgroundColorImage;

        [Header("Player Panels (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")] [SerializeField]
        private RectTransform[] _playerSlots;

        [SerializeField] private Image[] _playerIcons;
        [SerializeField] private Slider[] _scoreSliders;

        [Header("Assets")] [SerializeField] private Sprite[] _playerDefaultSprites;
        [SerializeField] private Sprite[] _playerWinnerIcons;
        [SerializeField] private Sprite[] _winnerTitleSprites;
        [SerializeField] private Sprite[] _winnerBackgrounds;
        [SerializeField] private Sprite[] _winnerBackgroundColors;

        [Header("Animation Settings")] [SerializeField]
        private float _sliderAnimationDuration = 0.3f;

        [SerializeField] private float _reorderPlayerDelay = 0.3f;

        [Header("Tween Settings")] [SerializeField]
        private float _leaderboardMoveDuration = 0.5f;

        [SerializeField] private float _topPlayerScaleDuration = 0.3f;
        [SerializeField] private float _topPlayerScaleFactor = 1.2f;
        [SerializeField] private Ease _leaderboardTweenEase = Ease.OutBack;
        [SerializeField] private Ease _scaleTweenEase = Ease.OutBack;

        [Header("Leaderboard Positions")] [SerializeField]
        private Vector2[] _leaderboardPositions;

        [SerializeField] private Sprite[] _placeSprites;
        [SerializeField] private Sprite[] _scoreSprites;

        [Header("Kill Assets")] [SerializeField]
        private Sprite _bombshellKillContextSprite;

        [SerializeField] private Sprite _bombshellKillScoreSprite;
        [SerializeField] private Sprite _fallKillContextSprite;
        [SerializeField] private Sprite _fallKillScoreSprite;
        [SerializeField] private Sprite _vehicleCrashKillContextSprite;
        [SerializeField] private Sprite _vehicleCrashKillScoreSprite;

        [SerializeField] private Image[] _scoreImages;
        [SerializeField] private Image[] _killContextImages;
        [SerializeField] private Image[] _placeImages;

        [SerializeField] private float _hidePlacementScaleDuration = 1f;
        [SerializeField] private float _showPlacementScaleDuration = 1f;
        [SerializeField] private float _startKillAnimDelay = 0.5f;
        [SerializeField] private float _hideDelay = 0.2f;
        [SerializeField] private float _updateSlidersDelay = 0.2f;
        [SerializeField] private float _hideKillScaleDuration = 0.5f;
        [SerializeField] private float _showKillScaleDuration = 0.5f;

        [SerializeField] private Ease _showPlacementEase = Ease.OutBack;
        [SerializeField] private Ease _hidePlacementEase = Ease.InBack;
        [SerializeField] private Ease _showKillEase = Ease.OutBack;
        [SerializeField] private Ease _hideKillEase = Ease.InBack;

        private int[] _leaderboardOrder;
        private Vector3 _originalScale;
        private int _previousTopPlayerIndex = 0;
        private int[] _displayedScores;

        private void Awake()
        {
            _originalScale = _playerSlots[0].transform.localScale;
            _leaderboardOrder = Enumerable.Range(0, _playerSlots.Length).ToArray();
            ResetUI();
            SetPlayersToLeaderboardOrder(_leaderboardOrder);
        }

        public void OnRoundEnded(RoundInfo round, GameModeBase gm)
        {
            ResetUI();
            var teams = gm.Teams;

            _displayedScores = new int[_playerSlots.Length];
            foreach (var team in teams)
            {
                _displayedScores[team.Index] =
                    team.Score
                    - GetPlacementBonus(team, gm)
                    - GetTotalKillScore(team, gm);
            }

            InitializePlayerPanels(teams, _leaderboardOrder);
            ShowRoundWinner(round.WinningTeam);

            AnimateRoundEndSequence(teams, gm).Forget();
        }

        #region Animate Sliders / Placement / Kills

        private async UniTask AnimatePlacementText(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            var showTasks = new List<UniTask>();
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                int rankIndex = team.Rank - 1;
                if (_scoreSliders[idx] != null)
                    _scoreSliders[idx].maxValue = gm.Data.ScoreToWin;

                _placeImages[idx].transform.localScale = Vector3.zero;
                _scoreImages[idx].transform.localScale = Vector3.zero;

                _placeImages[idx].sprite = _placeSprites[rankIndex];
                _scoreImages[idx].sprite = _scoreSprites[rankIndex];

                _placeImages[idx].SetNativeSize();
                _scoreImages[idx].SetNativeSize();
                
                _placeImages[idx].gameObject.SetActive(true);
                _scoreImages[idx].gameObject.SetActive(true);

                showTasks.Add(
                    Tween.Scale(_placeImages[idx].transform, Vector3.one, _showPlacementScaleDuration,
                            _showPlacementEase)
                        .Group(Tween.Scale(_scoreImages[idx].transform, Vector3.one, _showPlacementScaleDuration,
                            _showPlacementEase))
                        .ToUniTask()
                );
            }

            await UniTask.WhenAll(showTasks);

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay));

            var hideTasks = new List<UniTask>();
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                hideTasks.Add(
                    Tween.Scale(_placeImages[idx].transform, Vector3.zero, _hidePlacementScaleDuration,
                            _hidePlacementEase)
                        .Group(Tween.Scale(_scoreImages[idx].transform, Vector3.zero, _hidePlacementScaleDuration,
                            _hidePlacementEase))
                        .ToUniTask()
                );
            }

            await UniTask.WhenAll(hideTasks);

            await UniTask.Delay(TimeSpan.FromSeconds(_updateSlidersDelay));

            var sliderTasks = new List<UniTask>();
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                int bonus = GetPlacementBonus(team, gm);
                if (bonus <= 0) continue;

                int start = _displayedScores[idx];
                int end = start + bonus;
                _displayedScores[idx] = end;

                sliderTasks.Add(AnimateSlider(_scoreSliders[idx], start, end, _sliderAnimationDuration));
            }

            if (sliderTasks.Count > 0)
                await UniTask.WhenAll(sliderTasks);

            await UniTask.Delay(TimeSpan.FromSeconds(_startKillAnimDelay));

            await AnimateKillsByRound(teams, gm);
        }

        private async UniTask AnimateKillsByRound(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            int maxKills = teams.Max(t => t.Members.Sum(m => m.Metrics.RoundKills.Count));

            for (int killRound = 0; killRound < maxKills; killRound++)
            {
                var showTasks = new List<UniTask>();
                foreach (var team in teams)
                {
                    int idx = team.Index;
                    if (!IsValidPlayerIndex(idx)) continue;

                    var kills = team.Members.SelectMany(m => m.Metrics.RoundKills).ToList();
                    if (killRound >= kills.Count) continue;

                    var cause = kills[killRound];
                    var contextImg = _killContextImages[idx];
                    var scoreImg = _scoreImages[idx];

                    contextImg.sprite = GetKillContextSprite(cause);
                    scoreImg.sprite = GetKillScoreSprite(cause);

                    contextImg.transform.localScale = Vector3.zero;
                    scoreImg.transform.localScale = Vector3.zero;

                    contextImg.SetNativeSize();
                    scoreImg.SetNativeSize();
                    
                    contextImg.gameObject.SetActive(true);
                    scoreImg.gameObject.SetActive(true);

                    showTasks.Add(
                        Tween.Scale(contextImg.transform, Vector3.one, _showKillScaleDuration, _showKillEase)
                            .Group(Tween.Scale(scoreImg.transform, Vector3.one, _showKillScaleDuration, _showKillEase))
                            .ToUniTask()
                    );
                }

                if (showTasks.Count > 0)
                    await UniTask.WhenAll(showTasks);

                await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay));

                var hideTasks = new List<UniTask>();
                foreach (var team in teams)
                {
                    int idx = team.Index;
                    if (!IsValidPlayerIndex(idx)) continue;

                    var contextImg = _killContextImages[idx];
                    var scoreImg = _scoreImages[idx];
                    if (!contextImg.gameObject.activeSelf) continue;

                    hideTasks.Add(
                        Tween.Scale(contextImg.transform, Vector3.zero, _hideKillScaleDuration, _hideKillEase)
                            .Group(Tween.Scale(scoreImg.transform, Vector3.zero, _hideKillScaleDuration, _hideKillEase))
                            .ToUniTask()
                    );
                }

                if (hideTasks.Count > 0)
                    await UniTask.WhenAll(hideTasks);

                foreach (var team in teams)
                {
                    int idx = team.Index;
                    if (!IsValidPlayerIndex(idx)) continue;
                    _killContextImages[idx].gameObject.SetActive(false);
                    _scoreImages[idx].gameObject.SetActive(false);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay));

                await UniTask.Delay(TimeSpan.FromSeconds(_updateSlidersDelay));

                var sliderTasks = new List<UniTask>();
                foreach (var team in teams)
                {
                    int idx = team.Index;
                    if (!IsValidPlayerIndex(idx)) continue;

                    var kills = team.Members.SelectMany(m => m.Metrics.RoundKills).ToList();
                    if (killRound >= kills.Count) continue;

                    var cause = kills[killRound];
                    int start = _displayedScores[idx];
                    int end = start + GetKillScore(cause, gm);
                    _displayedScores[idx] = end;

                    sliderTasks.Add(AnimateSlider(_scoreSliders[idx], start, end, _sliderAnimationDuration));
                }

                if (sliderTasks.Count > 0)
                    await UniTask.WhenAll(sliderTasks);

                await UniTask.Delay(TimeSpan.FromSeconds(_hideDelay));
            }
        }

        private Sprite GetKillContextSprite(E_DeathCause cause)
        {
            return cause switch
            {
                E_DeathCause.BombshellExplosion => _bombshellKillContextSprite,
                E_DeathCause.Fall => _fallKillContextSprite,
                E_DeathCause.VehicleCrash => _vehicleCrashKillContextSprite,
                _ => null
            };
        }

        private Sprite GetKillScoreSprite(E_DeathCause cause)
        {
            return cause switch
            {
                E_DeathCause.BombshellExplosion => _bombshellKillScoreSprite,
                E_DeathCause.Fall => _fallKillScoreSprite,
                E_DeathCause.VehicleCrash => _vehicleCrashKillScoreSprite,
                _ => null
            };
        }

        private async UniTask AnimateSlider(Slider slider, float start, float end, float duration)
        {
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_ScoreIncrease);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                slider.value = Mathf.Lerp(start, end, elapsed / duration);
                await UniTask.Yield();
            }

            slider.value = end;
        }

        #endregion

        #region Leaderboard / Helpers

        private async UniTask AnimateRoundEndSequence(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            await AnimatePlacementText(teams, gm);
            await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay));

            var sortedTeams = teams.OrderByDescending(t => t.Score).ToList();
            await AnimateLeaderboardPositions(sortedTeams);
            _leaderboardOrder = sortedTeams.Select(t => t.Index).ToArray();
        }

        private async UniTask AnimateLeaderboardPositions(List<PlayerTeam> sortedTeams)
        {
            var animations = new UniTask[sortedTeams.Count];

            int currentTopIdx = sortedTeams[0].Index;
            bool isSameTopPlayer = currentTopIdx == _previousTopPlayerIndex;

            if (!isSameTopPlayer)
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_NewLeader);
                await Tween.Scale(_playerSlots[currentTopIdx], _originalScale * _topPlayerScaleFactor,
                    _topPlayerScaleDuration, _scaleTweenEase);
            }

            for (int rank = 0; rank < sortedTeams.Count; rank++)
            {
                int playerIdx = sortedTeams[rank].Index;
                animations[rank] = TweenPlayerToPosition(_playerSlots[playerIdx], _leaderboardPositions[rank],
                    _leaderboardMoveDuration, _leaderboardTweenEase);
            }

            await UniTask.WhenAll(animations);

            if (!isSameTopPlayer)
                await Tween.Scale(_playerSlots[currentTopIdx], _originalScale, _topPlayerScaleDuration,
                    _scaleTweenEase);

            _previousTopPlayerIndex = currentTopIdx;
        }

        private async UniTask TweenPlayerToPosition(RectTransform rt, Vector2 target, float duration, Ease ease)
        {
            if (Vector2.Distance(rt.anchoredPosition, target) < 0.01f) return;
            await Tween.UIAnchoredPosition(rt, target, duration, ease);
        }

        private void SetPlayersToLeaderboardOrder(int[] order)
        {
            for (int rank = 0; rank < order.Length; rank++)
            {
                int playerIdx = order[rank];
                if (!IsValidPlayerIndex(playerIdx)) continue;
                _playerSlots[playerIdx].anchoredPosition = _leaderboardPositions[rank];
            }
        }

        private void InitializePlayerPanels(ReadOnlyCollection<PlayerTeam> teams, int[] orderOverride = null)
        {
            for (int i = 0; i < _playerSlots.Length; i++)
                _playerSlots[i].gameObject.SetActive(false);

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                _playerSlots[idx].gameObject.SetActive(true);
                _playerIcons[idx].sprite = _playerDefaultSprites[idx];
                _playerIcons[idx].gameObject.SetActive(true);
                _playerSlots[idx].transform.localScale = _originalScale;
            }

            if (orderOverride != null)
                SetPlayersToLeaderboardOrder(orderOverride);

            var topTeam = teams.OrderByDescending(t => t.Score).FirstOrDefault();
            if (topTeam != null && IsValidPlayerIndex(topTeam.Index))
                _playerIcons[topTeam.Index].sprite = _playerWinnerIcons[topTeam.Index];
        }

        private void ShowRoundWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null || !IsValidPlayerIndex(winningTeam.Index)) return;

            _winnerTitleImage.sprite = _winnerTitleSprites[winningTeam.Index];
            _winnerBackgroundImage.sprite = _winnerBackgrounds[winningTeam.Index];
            _winnerBackgroundColorImage.sprite = _winnerBackgroundColors[winningTeam.Index];

            _winnerTitleImage.gameObject.SetActive(true);
            _winnerBackgroundImage.gameObject.SetActive(true);
            _winnerBackgroundColorImage.gameObject.SetActive(true);
        }

        private int GetPlacementBonus(PlayerTeam team, GameModeBase gm)
        {
            return team.Rank switch
            {
                1 => gm.Data.FirstRankBonusScore,
                2 => gm.Data.SecondRankBonusScore,
                3 => gm.Data.ThirdRankBonusScore,
                _ => 0
            };
        }

        private int GetKillScore(E_DeathCause cause, GameModeBase gm)
        {
            return cause switch
            {
                E_DeathCause.BombshellExplosion => gm.Data.KillBonusScore,
                E_DeathCause.Fall => gm.Data.KillBonusScore + gm.Data.KillPushBonusScore,
                E_DeathCause.VehicleCrash => gm.Data.KillBonusScore + gm.Data.KillCarCrashBonusScore,
                _ => 0
            };
        }

        private int GetTotalKillScore(PlayerTeam team, GameModeBase gm)
        {
            int total = 0;
            foreach (var member in team.Members)
            {
                total += member.Metrics.RoundKills.Sum(k => GetKillScore(k, gm));
            }

            return total;
        }

        private bool IsValidPlayerIndex(int idx) => idx >= 0 && idx < _playerSlots.Length;

        public void ResetUI()
        {
            _winnerTitleImage.gameObject.SetActive(false);
            _winnerBackgroundImage.gameObject.SetActive(false);
            _winnerBackgroundColorImage.gameObject.SetActive(false);

            for (int i = 0; i < _playerIcons.Length; i++)
            {
                _playerSlots[i].gameObject.SetActive(false);
                _scoreImages[i].gameObject.SetActive(false);
                _placeImages[i].gameObject.SetActive(false);
                _killContextImages[i].gameObject.SetActive(false);

                if (i < _playerIcons.Length)
                    _playerIcons[i].sprite = _playerDefaultSprites[i];
            }
        }

        #endregion
    }
}