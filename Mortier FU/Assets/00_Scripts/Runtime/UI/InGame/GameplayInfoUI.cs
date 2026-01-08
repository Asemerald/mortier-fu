using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class GameplayInfoUI : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private TextMeshProUGUI _roundText;

        public void OnRoundStarted(RoundInfo currentRound)
        {
            UpdateRoundText(currentRound.RoundIndex);
        }

        private void UpdateRoundText(int currentRound)
        {
            if (_roundText != null)
                _roundText.text = $"Round #{currentRound}";
        }
    }
}