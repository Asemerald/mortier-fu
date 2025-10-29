using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public class GameplayUI : MonoBehaviour
    {
        [SerializeField] private Transform _scorePanelGroup;
        [SerializeField] private ScorePanel _scorePanelPrefab;
        private ScorePanel[] _scorePanels;
        
        public void Initialize(PlayerTeam[] teams)
        {
            _scorePanelGroup.DestroyChildren();
            
            _scorePanels = new ScorePanel[teams.Length];
            for (int i = 0; i < teams.Length; i++)
            {
                var panel = Instantiate(_scorePanelPrefab, _scorePanelGroup);
                panel.Initialize(teams[i]);
            }
            
            Hide();
        }

        public void UpdateScores()
        {
            foreach (var panel in _scorePanels)
            {
                panel.UpdateData();
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}