using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")] [SerializeField] private Image _winnerImage;
        [SerializeField] private Image _winnerImageBackground;

        [Header("Players UI (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")] [SerializeField]
        private Image[] playerImages;

        [SerializeField] private TMP_Text[] scoreTexts;

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
        }

        private void DisplayPlayers(ReadOnlyCollection<PlayerTeam> teams)
        {
            for (int i = 0; i < playerImages.Length; i++)
            {
                playerImages[i].gameObject.SetActive(false);
                scoreTexts[i].gameObject.SetActive(false);
            }

            foreach (var team in teams)
            {
                int index = team.Index;

                if (index < 0 || index >= playerImages.Length)
                    continue;

                playerImages[index].gameObject.SetActive(true);
                scoreTexts[index].gameObject.SetActive(true);

                playerImages[index].sprite = defaultSprites[index];
                scoreTexts[index].text = team.Score.ToString();
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

            playerImages[index].sprite = _winnerIconSprites[index];
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
                scoreTexts[i].gameObject.SetActive(false);

                if (i < defaultSprites.Length)
                    playerImages[i].sprite = defaultSprites[i];
            }
        }
    }
}