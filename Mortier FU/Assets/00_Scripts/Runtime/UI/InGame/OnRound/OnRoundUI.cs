using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class OnRoundUI : MonoBehaviour
    {
        [SerializeField] private RoundAnnouncementUI _roundAnnouncementUI;
        
        [SerializeField] private CountdownUI _countdownUI;
        
        private GameModeBase _gm;

        private void Awake()
        {
            
            _roundAnnouncementUI.gameObject.SetActive(false);
            _countdownUI.gameObject.SetActive(false);
        }
        
        private void OnEnable()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
            {
                Logs.LogWarning("[RoundAnnouncementUI]: Game mode not found");
                return;
            }

            _gm.OnGameStarted += _roundAnnouncementUI.OnGameStarted;
            _gm.OnRoundStarted += StartRound;
        }
        
        private void OnDisable()
        {
            if (_gm == null) return;

            _gm.OnGameStarted -= _roundAnnouncementUI.OnGameStarted;
            _gm.OnRoundStarted -= StartRound;
        }

        private void StartRound(RoundInfo currentRound)
        {
            _roundAnnouncementUI.gameObject.SetActive(true);
            _roundAnnouncementUI.OnRoundStarted();
            _countdownUI.gameObject.SetActive(true);
            _countdownUI.PlayCountdown().Forget();
        }
    }
}