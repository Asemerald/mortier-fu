using System;
using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")]
        [SerializeField] private Image _winnerImage;
        [SerializeField] private Image _winnerImageBackground;

        [Header("Players UI (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")]
        [SerializeField] private Image[] playerImages;
        [SerializeField] private TextMeshProUGUI[] _placeTexts;
        [SerializeField] private Slider[] _sliders;

        [Header("Assets")]
        [SerializeField] private Sprite[] defaultSprites;
        [SerializeField] private Sprite[] _winnerIconSprites;
        [SerializeField] private Sprite[] _winnerTextSprites;
        [SerializeField] private Sprite[] _winnerBackgroundSprites;

        [SerializeField] private float _animateSliderDelay = 1f;
        [SerializeField] private float _sliderAnimationDuration = 1f;
        [SerializeField] private float _reorderPlayerDelay = 0.3f;

        [Header("Leaderboard Positions")]
        [SerializeField] private Vector2[] _leaderboardAnchoredPositions; 

        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = playerImages[0].transform.localScale;
            ResetUI();

            for (int i = 0; i < playerImages.Length; i++)
            {
                playerImages[i].rectTransform.anchoredPosition = _leaderboardAnchoredPositions[i];
            }
        }

        public void OnRoundEnded(RoundInfo round, GameModeBase gm)
        {
            ResetUI();
            var teams = gm.Teams;

            DisplayPlayers(teams);
            DisplayWinner(round.WinningTeam);

            ShowPlayerPlacesAndScore(teams, gm).Forget();
        }

        private async UniTask ShowPlayerPlacesAndScore(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            foreach (var team in teams)
            {
                int idx = team.Index;
                if (idx < 0 || idx >= _placeTexts.Length) continue;
                _placeTexts[idx].text = GetPlaceText(team.Rank, gm.GetScorePerRank(team.Rank));
                if (_sliders[idx] != null) _sliders[idx].maxValue = gm.Data.ScoreToWin;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_animateSliderDelay));

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (_sliders[idx] != null)
                    await AnimateSlider(_sliders[idx], _sliders[idx].value, team.Score, _sliderAnimationDuration);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_reorderPlayerDelay));

            var sortedTeams = teams.OrderByDescending(t => t.Score).ToList();

            await AnimateLeaderboard(sortedTeams);
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

        private string GetPlaceText(int rank, int rankScore)
        {
            string rankText = rank switch
            {
                1 => "1st Place",
                2 => "2nd Place",
                3 => "3rd Place",
                4 => "4th Place",
                _ => rank + "th Place"
            };
            return $"{rankText} + {rankScore}";
        }

        private void DisplayPlayers(ReadOnlyCollection<PlayerTeam> teams)
        {
            for (int i = 0; i < playerImages.Length; i++)
                playerImages[i].gameObject.SetActive(false);

            foreach (var team in teams)
            {
                int idx = team.Index;
                if (idx < 0 || idx >= playerImages.Length) continue;
                playerImages[idx].gameObject.SetActive(true);
                playerImages[idx].sprite = defaultSprites[idx];
            }

            var highestTeam = teams.OrderByDescending(t => t.Score).FirstOrDefault();
            if (highestTeam != null && highestTeam.Index >= 0 && highestTeam.Index < playerImages.Length)
                playerImages[highestTeam.Index].sprite = _winnerIconSprites[highestTeam.Index];
        }

        private void DisplayWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null) return;

            int idx = winningTeam.Index;
            if (idx < 0 || idx >= _winnerTextSprites.Length)
            {
                Logs.LogError("[RoundEndUI] Invalid winning team index");
                return;
            }

            _winnerImage.sprite = _winnerTextSprites[idx];
            _winnerImageBackground.sprite = _winnerBackgroundSprites[idx];
            _winnerImage.gameObject.SetActive(true);
            _winnerImageBackground.gameObject.SetActive(true);
        }

        private async UniTask AnimateLeaderboard(System.Collections.Generic.List<PlayerTeam> sortedTeams)
        {
            UniTask[] animations = new UniTask[sortedTeams.Count];

            int topIdx = sortedTeams[0].Index;
            await Tween.Scale(playerImages[topIdx].transform, _originalScale * 1.2f, 0.3f, Ease.OutBack);

            for (int rank = 0; rank < sortedTeams.Count; rank++)
            {
                int playerIdx = sortedTeams[rank].Index;
                var rt = playerImages[playerIdx].rectTransform;

                Vector2 target = _leaderboardAnchoredPositions[rank];
                animations[rank] = TweenToAnchoredPositionSafe(rt, target, 0.5f);
            }

            await UniTask.WhenAll(animations);
            await Tween.Scale(playerImages[topIdx].transform, _originalScale, 0.3f, Ease.OutBack);
        }

        private UniTask TweenToAnchoredPositionSafe(RectTransform rt, Vector2 target, float duration)
        {
            if (Vector2.Distance(rt.anchoredPosition, target) < 0.01f)
                return UniTask.CompletedTask;

            return Tween.UIAnchoredPosition(rt, target, duration, Ease.OutBack).ToUniTask();
        }

        public void ResetUI()
        {
            _winnerImage.gameObject.SetActive(false);
            _winnerImageBackground.gameObject.SetActive(false);
            _winnerImage.material = null;
            _winnerImageBackground.material = null;

            for (int i = 0; i < playerImages.Length; i++)
            {
                playerImages[i].gameObject.SetActive(false);
                if (i < defaultSprites.Length)
                    playerImages[i].sprite = defaultSprites[i];

                if (i < _leaderboardAnchoredPositions.Length)
                    playerImages[i].rectTransform.anchoredPosition = _leaderboardAnchoredPositions[i];
            }
        }
    }
}
