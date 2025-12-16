using MortierFu.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;

namespace MortierFu
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Winner UI")] [SerializeField] private Image winnerImage;
        [SerializeField] private Image winnerImageBackground;

        [Header("Players UI (0 = Blue, 1 = Red, 2 = Green, 3 = Yellow)")] [SerializeField]
        private Image[] playerImages;

        [SerializeField] private TMP_Text[] scoreTexts;

        [Header("Assets")] [SerializeField] private Sprite[] defaultSprites;
        [SerializeField] private Sprite[] _winnerIconSprites;
        [SerializeField] private Sprite[] _winnerTextSprites;
        [SerializeField] private Sprite[] _winnerBackgroundSprites;

        private GameModeBase _gm;

        private void Awake()
        {
            ResetUI();
        }

        private void OnEnable()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;
            if (_gm == null)
            {
                Logs.LogWarning("[RoundEndUI] Game mode not found");
                return;
            }

            _gm.OnRoundEnded += OnRoundEnded;
            _gm.OnScoreDisplayOver += HideScore;
        }

        private void OnDisable()
        {
            if (_gm == null) return;

            _gm.OnRoundEnded -= OnRoundEnded;
            _gm.OnScoreDisplayOver -= HideScore;
        }

        private void OnRoundEnded(RoundInfo round)
        {
            ResetUI();

            var teams = _gm.Teams;

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

            winnerImage.sprite = _winnerTextSprites[index];
            winnerImageBackground.sprite = _winnerBackgroundSprites[index];
            winnerImage.gameObject.SetActive(true);
            winnerImageBackground.gameObject.SetActive(true);

            playerImages[index].sprite = _winnerIconSprites[index];
        }

        private void HideScore()
        {
            ResetUI();
        }

        private void ResetUI()
        {
            winnerImage.gameObject.SetActive(false);
            winnerImageBackground.gameObject.SetActive(false);
            winnerImage.material = null;
            winnerImageBackground.material = null;

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