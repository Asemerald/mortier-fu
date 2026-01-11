using System;
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
        [Header("Winner UI")] [SerializeField] private Image _winnerTitleImage;
        [SerializeField] private Image _winnerBackgroundImage;

        [Header("Player Panels (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")] [SerializeField]
        private Image[] _playerSlots;

        [SerializeField] private TextMeshProUGUI[] _playerPlaceTexts;
        [SerializeField] private Slider[] _scoreSliders;

        [Header("Assets")] [SerializeField] private Sprite[] _playerDefaultSprites;
        [SerializeField] private Sprite[] _playerWinnerIcons;
        [SerializeField] private Sprite[] _winnerTitleSprites;
        [SerializeField] private Sprite[] _winnerBackgrounds;

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

        private Vector3 _originalScale;
        private int[] _leaderboardOrder;
        
        private int _previousTopPlayerIndex = 0;

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

            InitializePlayerPanels(teams, _leaderboardOrder);
            ShowRoundWinner(round.WinningTeam);

            DisplayKillIndicators(teams);

            AnimateRoundEndSequence(teams, gm).Forget();
        }

        private void DisplayKillIndicators(ReadOnlyCollection<PlayerTeam> teams)
        {
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                int roundKills = team.Members.Sum(m => m.Metrics.RoundKills);

                TextMeshProUGUI placeText = _playerPlaceTexts[idx];
                int maxIndicators = Mathf.Min(placeText.transform.childCount, 3);

                for (int i = 0; i < maxIndicators; i++)
                {
                    placeText.transform.GetChild(i).gameObject.SetActive(i < roundKills);
                }
            }
        }
        
        #endregion

        #region Slider & Leaderboard (existing code, unchanged)

        private async UniTask AnimateRoundEndSequence(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            UpdatePlayerPlaceAndScores(teams, gm);

            await UniTask.Delay(TimeSpan.FromSeconds(_animateSliderDelay));
            await AnimateScoreSliders(teams);

            await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay));

            var sortedTeams = teams.OrderByDescending(t => t.Score).ToList();

            await AnimateLeaderboardPositions(sortedTeams);

            _leaderboardOrder = sortedTeams.Select(t => t.Index).ToArray();
        }

        private void UpdatePlayerPlaceAndScores(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                _playerPlaceTexts[idx].text = GetPlaceText(team.Rank, gm.GetScorePerRank(team.Rank));
                if (_scoreSliders[idx] != null)
                    _scoreSliders[idx].maxValue = gm.Data.ScoreToWin;
            }
        }

        private async UniTask AnimateScoreSliders(ReadOnlyCollection<PlayerTeam> teams)
        {
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx) || _scoreSliders[idx] == null) continue;

                await AnimateSlider(_scoreSliders[idx], _scoreSliders[idx].value, team.Score, _sliderAnimationDuration);
            }
        }

        private async UniTask AnimateSlider(Slider slider, float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                slider.value = Mathf.Lerp(start, end, elapsed / duration);
                await UniTask.Yield();
            }

            slider.value = end;
        }

        private async UniTask AnimateLeaderboardPositions(System.Collections.Generic.List<PlayerTeam> sortedTeams)
        {
            var animations = new UniTask[sortedTeams.Count];

            int currentTopIdx = sortedTeams[0].Index;
            bool isSameTopPlayer = currentTopIdx == _previousTopPlayerIndex;
            
            if (!isSameTopPlayer)
            {
                await Tween.Scale(
                    _playerSlots[currentTopIdx].transform,
                    _originalScale * _topPlayerScaleFactor,
                    _topPlayerScaleDuration,
                    _scaleTweenEase
                );
            }

            for (int rank = 0; rank < sortedTeams.Count; rank++)
            {
                int playerIdx = sortedTeams[rank].Index;
                var rt = _playerSlots[playerIdx].rectTransform;
                animations[rank] = TweenPlayerToPosition(rt, _leaderboardPositions[rank], _leaderboardMoveDuration,
                    _leaderboardTweenEase);
            }

            await UniTask.WhenAll(animations);
            
            if (!isSameTopPlayer)
            {
                await Tween.Scale(
                    _playerSlots[currentTopIdx].transform,
                    _originalScale,
                    _topPlayerScaleDuration,
                    _scaleTweenEase
                );
            }
            
            _previousTopPlayerIndex = currentTopIdx;
        }

        private UniTask TweenPlayerToPosition(RectTransform rt, Vector2 target, float duration, Ease ease)
        {
            if (Vector2.Distance(rt.anchoredPosition, target) < 0.01f)
                return UniTask.CompletedTask;

            return Tween.UIAnchoredPosition(rt, target, duration, ease).ToUniTask();
        }

        private void SetPlayersToLeaderboardOrder(int[] order)
        {
            for (int rank = 0; rank < order.Length; rank++)
            {
                int playerIdx = order[rank];
                if (!IsValidPlayerIndex(playerIdx)) continue;

                _playerSlots[playerIdx].rectTransform.anchoredPosition = _leaderboardPositions[rank];
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
                _playerSlots[idx].sprite = _playerDefaultSprites[idx];
            }

            if (orderOverride != null)
                SetPlayersToLeaderboardOrder(orderOverride);

            var topTeam = teams.OrderByDescending(t => t.Score).FirstOrDefault();
            if (topTeam != null && IsValidPlayerIndex(topTeam.Index))
                _playerSlots[topTeam.Index].sprite = _playerWinnerIcons[topTeam.Index];
        }

        private void ShowRoundWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null || !IsValidPlayerIndex(winningTeam.Index))
                return;

            _winnerTitleImage.sprite = _winnerTitleSprites[winningTeam.Index];
            _winnerBackgroundImage.sprite = _winnerBackgrounds[winningTeam.Index];
            _winnerTitleImage.gameObject.SetActive(true);
            _winnerBackgroundImage.gameObject.SetActive(true);
        }

        private bool IsValidPlayerIndex(int idx) =>
            idx >= 0 && idx < _playerSlots.Length;

        private string GetPlaceText(int rank, int score)
        {
            string rankText = rank switch
            {
                1 => "1st Place",
                2 => "2nd Place",
                3 => "3rd Place",
                4 => "4th Place",
                _ => $"{rank}th Place"
            };
            return $"{rankText} + {score}";
        }

        public void ResetUI()
        {
            _winnerTitleImage.gameObject.SetActive(false);
            _winnerBackgroundImage.gameObject.SetActive(false);
            _winnerTitleImage.material = null;
            _winnerBackgroundImage.material = null;

            for (int i = 0; i < _playerSlots.Length; i++)
            {
                _playerSlots[i].gameObject.SetActive(false);
                if (i < _playerDefaultSprites.Length)
                    _playerSlots[i].sprite = _playerDefaultSprites[i];

                if (i < _leaderboardPositions.Length)
                    _playerSlots[i].rectTransform.anchoredPosition = _leaderboardPositions[i];

                if (_playerPlaceTexts == null || i >= _playerPlaceTexts.Length) continue;
                TextMeshProUGUI placeText = _playerPlaceTexts[i];
                for (int k = 0; k < placeText.transform.childCount; k++)
                    placeText.transform.GetChild(k).gameObject.SetActive(false);
            }
        }

        #endregion
    }
}