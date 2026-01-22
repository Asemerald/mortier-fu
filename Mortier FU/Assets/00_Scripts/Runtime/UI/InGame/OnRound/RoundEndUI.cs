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
        [SerializeField] private Sprite[] _killContextSprites;
        [SerializeField] private Sprite[] _scoreSprites;

        [SerializeField] private Image[] _scoreImages;
        [SerializeField] private Image[] _killContextImages;
        [SerializeField] private Image[] _placeImages;
        [SerializeField] private float _placementDisplayDuration = 2f;
        private int[] _leaderboardOrder;

        private Vector3 _originalScale;

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

                int roundKills = team.Members.Sum(m => m.Metrics.RoundKills.Count);

                /*TextMeshProUGUI placeText = _playerPlaceTexts[idx];
                int maxIndicators = Mathf.Min(placeText.transform.childCount, 3);

                for (int i = 0; i < maxIndicators; i++)
                {
                    placeText.transform.GetChild(i).gameObject.SetActive(i < roundKills);
                }*/
            }
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

            await AnimateScoreSliders(teams);
            
            
         //   int roundKills = team.Members.Sum(m => m.Metrics.RoundKills.Count);
        }

        private async UniTask AnimateRoundEndSequence(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            await AnimatePlacementText(teams, gm);

            /* UpdatePlayerPlaceAndScores(teams, gm);

             ShowPlacementInfo(teams, gm);
             await UniTask.Delay(TimeSpan.FromSeconds(_placementDisplayDuration));

             HidePlacementInfo();

             await UniTask.Delay(TimeSpan.FromSeconds(_animateSliderDelay));
             await AnimateScoreSliders(teams);

             await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay));

             var sortedTeams = teams.OrderByDescending(t => t.Score).ToList();
             await AnimateLeaderboardPositions(sortedTeams);

             _leaderboardOrder = sortedTeams.Select(t => t.Index).ToArray();*/
        }

        private void UpdatePlayerPlaceAndScores(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx)) continue;

                /* _playerPlaceTexts[idx].text = GetPlaceText(team.Rank, gm.GetScorePerRank(team.Rank));
                 */
            }
        }

        private async UniTask AnimateScoreSliders(ReadOnlyCollection<PlayerTeam> teams)
        {
            var tasks = new List<UniTask>();

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (!IsValidPlayerIndex(idx) || _scoreSliders[idx] == null) continue;

                tasks.Add(AnimateSlider(_scoreSliders[idx], _scoreSliders[idx].value, team.Score,
                    _sliderAnimationDuration));
            }

            await UniTask.WhenAll(tasks);
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

        private async UniTask AnimateLeaderboardPositions(System.Collections.Generic.List<PlayerTeam> sortedTeams)
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