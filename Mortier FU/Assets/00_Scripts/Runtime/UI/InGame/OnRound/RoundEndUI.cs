using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        private static readonly E_DeathCause[] KillDisplayOrder =
        {
            E_DeathCause.BombshellExplosion,
            E_DeathCause.Fall,
            E_DeathCause.VehicleCrash
        };

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
        private float _animateSliderDelay = 0.3f;

        [SerializeField] private float _sliderAnimationDuration = 0.3f;
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

        [SerializeField] private Sprite _bombshellKillContextSprite;
        [SerializeField] private Sprite _bombshellKillScoreSprite;
        [SerializeField] private Sprite _fallKillContextSprite;
        [SerializeField] private Sprite _fallKillScoreSprite;
        [SerializeField] private Sprite _vehicleCrashKillContextSprite;
        [SerializeField] private Sprite _vehicleCrashKillScoreSprite;

        [SerializeField] private Image[] _scoreImages;
        [SerializeField] private Image[] _killContextImages;
        [SerializeField] private Image[] _placeImages;
        [SerializeField] private float _placementDisplayDuration = 2f;
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

        #region Round End Display

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

        #endregion

        #region Slider & Leaderboard (existing code, unchanged)

        private async UniTask AnimatePlacementText(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            var tasks = new List<UniTask>();

            foreach (var team in teams)
            {
                int playerIdx = team.Index;
                int rankIndex = team.Rank - 1;

                if (!IsValidPlayerIndex(playerIdx)) continue;

                if (_scoreSliders[playerIdx] != null)
                    _scoreSliders[playerIdx].maxValue = gm.Data.ScoreToWin;

                _placeImages[playerIdx].gameObject.transform.localScale = Vector3.zero;
                _scoreImages[playerIdx].gameObject.transform.localScale = Vector3.zero;

                _placeImages[playerIdx].sprite = _placeSprites[rankIndex];
                _scoreImages[playerIdx].sprite = _scoreSprites[rankIndex];

                _placeImages[playerIdx].gameObject.SetActive(true);
                _scoreImages[playerIdx].gameObject.SetActive(true);

                tasks.Add(
                    Tween.Scale(_placeImages[playerIdx].transform, Vector3.one, 1f, Ease.OutBack)
                        .Group(Tween.Scale(_scoreImages[playerIdx].transform, Vector3.one, 1f, Ease.OutBack))
                        .ToUniTask()
                );
            }

            await UniTask.WhenAll(tasks);

            tasks.Clear();

            foreach (var team in teams)
            {
                int playerIdx = team.Index;

                if (!IsValidPlayerIndex(playerIdx)) continue;

                _placeImages[playerIdx].gameObject.transform.localScale = Vector3.one;
                _scoreImages[playerIdx].gameObject.transform.localScale = Vector3.one;

                tasks.Add(
                    Tween.Scale(_placeImages[playerIdx].transform, Vector3.zero, 1f, Ease.InBack)
                        .Group(Tween.Scale(_scoreImages[playerIdx].transform, Vector3.zero, 1f, Ease.InBack))
                        .ToUniTask()
                );
            }

            await UniTask.WhenAll(tasks);

            tasks.Clear();

            await AnimatePlacementScore(teams, gm);

            // Jusque là tout est good

            foreach (var cause in KillDisplayOrder)
            {
                var showTasks = new List<UniTask>();

                foreach (var team in teams)
                {
                    int playerIdx = team.Index;
                    if (!IsValidPlayerIndex(playerIdx))
                        continue;

                    int killCount = GetKillCountForCause(team, cause);
                    if (killCount <= 0)
                        continue;

                    var contextImg = _killContextImages[playerIdx];

                    var scoreImg = _scoreImages[playerIdx];

                    contextImg.transform.localScale = Vector3.zero;
                    scoreImg.transform.localScale = Vector3.zero;

                    contextImg.sprite = GetKillContextSprite(cause);
                    scoreImg.sprite = GetKillScoreSprite(cause);

                    contextImg.gameObject.SetActive(true);
                    scoreImg.gameObject.SetActive(true);

                    showTasks.Add(
                        Tween.Scale(contextImg.transform, Vector3.one, 0.5f, Ease.OutBack)
                            .Group(Tween.Scale(scoreImg.transform, Vector3.one, 0.5f, Ease.OutBack))
                            .ToUniTask()
                    );
                }

                if (showTasks.Count == 0)
                    continue;

                await UniTask.WhenAll(showTasks);

                await UniTask.Delay(TimeSpan.FromSeconds(0.6f));

                var hideTasks = new List<UniTask>();

                foreach (var team in teams)
                {
                    int playerIdx = team.Index;
                    if (!IsValidPlayerIndex(playerIdx))
                        continue;

                    var contextImg = _killContextImages[playerIdx];
                    var scoreImg = _scoreImages[playerIdx];

                    if (!contextImg.gameObject.activeSelf)
                        continue;

                    hideTasks.Add(
                        Tween.Scale(contextImg.transform, Vector3.zero, 0.4f, Ease.InBack)
                            .Group(Tween.Scale(scoreImg.transform, Vector3.zero, 0.4f, Ease.InBack))
                            .ToUniTask()
                    );
                }

                await UniTask.WhenAll(hideTasks);

                foreach (var team in teams)
                {
                    int playerIdx = team.Index;
                    if (!IsValidPlayerIndex(playerIdx))
                        continue;

                    _killContextImages[playerIdx].gameObject.SetActive(false);
                    _scoreImages[playerIdx].gameObject.SetActive(false);
                }

                await AnimateKillScoreForCause(teams, gm, cause);
                await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
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
        
        private int GetKillScore(E_DeathCause cause, GameModeBase gm)
        {
            return cause switch
            {
                E_DeathCause.BombshellExplosion =>
                    gm.Data.KillBonusScore,

                E_DeathCause.Fall =>
                    gm.Data.KillBonusScore + gm.Data.KillPushBonusScore,

                E_DeathCause.VehicleCrash =>
                    gm.Data.KillBonusScore + gm.Data.KillCarCrashBonusScore,

                _ => 0
            };
        }
        
        private int GetTotalKillScore(PlayerTeam team, GameModeBase gm)
        {
            int total = 0;

            foreach (var cause in KillDisplayOrder)
            {
                int count = GetKillCountForCause(team, cause);
                total += count * GetKillScore(cause, gm);
            }

            return total;
        }
        
        private async UniTask AnimateKillScoreForCause(
            ReadOnlyCollection<PlayerTeam> teams,
            GameModeBase gm,
            E_DeathCause cause)
        {
            var tasks = new List<UniTask>();
            int scorePerKill = GetKillScore(cause, gm);

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                int killCount = GetKillCountForCause(team, cause);
                if (killCount <= 0) continue;

                int gain = killCount * scorePerKill;
                int start = _displayedScores[idx];
                int end = start + gain;

                _displayedScores[idx] = end;

                tasks.Add(
                    AnimateSlider(_scoreSliders[idx], start, end, _sliderAnimationDuration)
                );
            }

            await UniTask.WhenAll(tasks);
        }

        private int GetKillCountForCause(PlayerTeam team, E_DeathCause cause)
        {
            return team.Members.Sum(m =>
                m.Metrics.RoundKills.Count(k => k == cause));
        }
        
        private async UniTask AnimatePlacementScore(
            ReadOnlyCollection<PlayerTeam> teams,
            GameModeBase gm)
        {
            var tasks = new List<UniTask>();

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                int bonus = GetPlacementBonus(team, gm);
                if (bonus <= 0) continue;

                int start = _displayedScores[idx];
                int end = start + bonus;

                _displayedScores[idx] = end;

                tasks.Add(
                    AnimateSlider(_scoreSliders[idx], start, end, _sliderAnimationDuration)
                );
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask AnimateRoundEndSequence(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            await AnimatePlacementText(teams, gm);

             await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay));

             var sortedTeams = teams.OrderByDescending(t => t.Score).ToList();
             await AnimateLeaderboardPositions(sortedTeams);

             _leaderboardOrder = sortedTeams.Select(t => t.Index).ToArray();
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

        private async UniTask AnimateLeaderboardPositions(List<PlayerTeam> sortedTeams)
        {
            var animations = new UniTask[sortedTeams.Count];

            int currentTopIdx = sortedTeams[0].Index;
            bool isSameTopPlayer = currentTopIdx == _previousTopPlayerIndex;

            if (!isSameTopPlayer)
            {
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_GameplayUI_NewLeader);
                await Tween.Scale(
                    _playerSlots[currentTopIdx],
                    _originalScale * _topPlayerScaleFactor,
                    _topPlayerScaleDuration,
                    _scaleTweenEase
                );
            }

            for (int rank = 0; rank < sortedTeams.Count; rank++)
            {
                int playerIdx = sortedTeams[rank].Index;
                var rt = _playerSlots[playerIdx];
                animations[rank] = TweenPlayerToPosition(rt, _leaderboardPositions[rank], _leaderboardMoveDuration,
                    _leaderboardTweenEase);
            }

            await UniTask.WhenAll(animations);

            if (!isSameTopPlayer)
            {
                await Tween.Scale(
                    _playerSlots[currentTopIdx],
                    _originalScale,
                    _topPlayerScaleDuration,
                    _scaleTweenEase
                );
            }

            _previousTopPlayerIndex = currentTopIdx;
        }

        private async UniTask TweenPlayerToPosition(RectTransform rt, Vector2 target, float duration, Ease ease)
        {
            if (Vector2.Distance(rt.anchoredPosition, target) < 0.01f)
                return;

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

        #endregion

        #region Display / Helpers

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
            if (winningTeam == null || !IsValidPlayerIndex(winningTeam.Index))
                return;

            _winnerTitleImage.sprite = _winnerTitleSprites[winningTeam.Index];
            _winnerBackgroundImage.sprite = _winnerBackgrounds[winningTeam.Index];
            _winnerBackgroundColorImage.sprite = _winnerBackgroundColors[winningTeam.Index];

            _winnerTitleImage.gameObject.SetActive(true);
            _winnerBackgroundImage.gameObject.SetActive(true);
            _winnerBackgroundColorImage.gameObject.SetActive(true);
        }

        private bool IsValidPlayerIndex(int idx) =>
            idx >= 0 && idx < _playerSlots.Length;


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