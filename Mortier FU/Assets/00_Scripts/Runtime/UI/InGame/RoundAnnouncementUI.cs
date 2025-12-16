using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MortierFu
{
    public class RoundAnnouncementUI : MonoBehaviour
    {
        [SerializeField] private GameObject _goldenBombshellGameObject;

        [SerializeField] private CountdownUI _countdownUI;

        private void Awake()
        {
            _countdownUI.gameObject.SetActive(false);
        }

        public void OnGameStarted()
        {
            InitializeUI();
        }

        public void OnRoundStarted(GameModeBase gm)
        {
            UpdateMatchPointIndicator(gm);

            _countdownUI.gameObject.SetActive(true);
            _countdownUI.PlayCountdown().Forget();
        }

        private void InitializeUI()
        {
            gameObject.SetActive(false);

            _goldenBombshellGameObject.SetActive(false);
        }

        private void UpdateMatchPointIndicator(GameModeBase gm)
        {
            if (gm == null || _goldenBombshellGameObject.activeSelf) return;

            bool isMatchPoint = false;

            for (int i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i].Score < gm.Data.ScoreToWin) continue;
                isMatchPoint = true;
                break;
            }

            _goldenBombshellGameObject.SetActive(isMatchPoint);
        }
    }
}