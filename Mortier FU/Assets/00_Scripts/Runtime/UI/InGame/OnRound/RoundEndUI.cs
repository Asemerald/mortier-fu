using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")] [SerializeField] private Image _winnerImage;
        [SerializeField] private Image _winnerImageBackground;


        [Header("Players UI (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")] [SerializeField]
        private Image[] playerImages;

        [SerializeField] private TextMeshProUGUI[] _placeTexts;
        [SerializeField] private Slider[] _sliders;

        [Header("Assets")] [SerializeField] private Sprite[] defaultSprites;
        [SerializeField] private Sprite[] _winnerIconSprites;
        [SerializeField] private Sprite[] _winnerTextSprites;
        [SerializeField] private Sprite[] _winnerBackgroundSprites;

        private void Awake()
        {
            ResetUI();
        }

        public void OnRoundEnded(RoundInfo round, GameModeBase gm)
        {
            ResetUI();

            var teams = gm.Teams;

            DisplayPlayers(teams);
            DisplayWinner(round.WinningTeam);
            UpdatePlaceTexts(teams, gm);
        }

        private async UniTask ShowPlayerPlaces(RoundInfo round, GameModeBase gm)
        {
            var teams = gm.Teams;
            DisplayPlayers(teams);
            DisplayWinner(round.WinningTeam);

            UpdatePlaceTexts(teams, gm);

            await UniTask.Delay(2000);
        }

        private void UpdatePlaceTexts(ReadOnlyCollection<PlayerTeam> teams, GameModeBase gm)
        {
            foreach (var team in teams)
            {
                int index = team.Index;

                if (index < 0 || index >= _placeTexts.Length) continue;
                
                int rankScore = gm.GetScorePerRank(team.Rank);
                _placeTexts[index].text = GetPlaceText(team.Rank, rankScore);
            }
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
            {
                playerImages[i].gameObject.SetActive(false);
            }

            foreach (var team in teams)
            {
                int index = team.Index;

                if (index < 0 || index >= playerImages.Length)
                    continue;

                playerImages[index].gameObject.SetActive(true);

                playerImages[index].sprite = defaultSprites[index];
            }

            // Team with the highest score  change his sprite to winner icon
            PlayerTeam highestScoreTeam = null;
            foreach (var team in teams)
            {
                if (highestScoreTeam == null || team.Score > highestScoreTeam.Score)
                {
                    highestScoreTeam = team;
                }
            }

            if (highestScoreTeam == null) return;
            int highestIndex = highestScoreTeam.Index;
            if (highestIndex >= 0 && highestIndex < playerImages.Length)
            {
                playerImages[highestIndex].sprite = _winnerIconSprites[highestIndex];
            }
        }

        private void DisplayWinner(PlayerTeam winningTeam)
        {
            if (winningTeam == null)
                return;

            int index = winningTeam.Index;
            if (index < 0 || index >= _winnerTextSprites.Length)
            {
                Logs.LogError("[RoundEndUI] Invalid winning team index");
                return;
            }

            _winnerImage.sprite = _winnerTextSprites[index];
            _winnerImageBackground.sprite = _winnerBackgroundSprites[index];
            _winnerImage.gameObject.SetActive(true);
            _winnerImageBackground.gameObject.SetActive(true);

            // TODO: Set active false le gameobject
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
            }
        }
    }
}