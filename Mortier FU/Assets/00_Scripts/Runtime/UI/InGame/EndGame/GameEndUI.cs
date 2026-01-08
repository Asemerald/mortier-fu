using UnityEngine.UI;
using UnityEngine;

namespace MortierFu
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _winnerImage;

        [SerializeField] private Image _winnerImageBackground;
        [SerializeField] private Image[] _playerImages;

        [Header("Assets")] [SerializeField] private Sprite[] _winnerTextSprites;
        [SerializeField] private Sprite[] _winnerBackgroundSprites;
        [SerializeField] private Sprite[] _winnerIconSprites;

        public void DisplayVictoryScreen(int winnerIndex, int activePlayerCount)
        {
            DisplayPlayerImages(activePlayerCount, winnerIndex);

            SetWinner(winnerIndex);
        }

        private void DisplayPlayerImages(int playerCount, int winnerIndex)
        {
            for (int i = 0; i < _playerImages.Length; i++)
            {
                _playerImages[i].gameObject.SetActive(i < playerCount);
            }

            for (int i = 0; i < playerCount; i++)
            {
                if (i == winnerIndex)
                {
                    _playerImages[i].sprite = _winnerIconSprites[i];
                }
            }
        }

        private void SetWinner(int playerIndex)
        {
            if (playerIndex >= 0 && playerIndex < _winnerTextSprites.Length)
            {
                _winnerImage.sprite = _winnerTextSprites[playerIndex];
                _winnerImageBackground.sprite = _winnerBackgroundSprites[playerIndex];
                _winnerImage.gameObject.SetActive(true);
                _winnerImageBackground.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("Invalid PlayerIndex");
            }
        }
    }
}