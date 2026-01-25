using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class RoundUI : MonoBehaviour
    {
        [SerializeField] private RoundAnnouncementUI _roundAnnouncementUI;
        [SerializeField] private GameEndUI _gameEndUI;
        
        private GameModeBase _gm;

        private void Awake()
        {
            _roundAnnouncementUI.gameObject.SetActive(false);
        }
        
        private void OnEnable()
        {
            _gm = GameService.CurrentGameMode as GameModeBase;

            if (_gm == null)
            {
                Logs.LogWarning("[RoundAnnouncementUI]: Game mode not found");
                return;
            }

            _gm.OnRoundStarted += HandleRoundStarted;
        }
        
        private void OnDisable()
        {
            if (_gm == null) return;

            _gm.OnRoundStarted -= HandleRoundStarted;
        }

        private void HandleRoundStarted(RoundInfo currentRound)
        {
            _roundAnnouncementUI.gameObject.SetActive(true);
            _roundAnnouncementUI.OnRoundStarted(_gm);
        }
        
    }
}