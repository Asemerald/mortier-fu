using TMPro;
using UnityEngine;

namespace MortierFu
{
    public class ScorePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreText;
        private PlayerTeam _team;

        public void Initialize(PlayerTeam team)
        {
            _team = team;
            UpdateData();
        }
        
        public void UpdateData()
        {
            _scoreText.text = $"Score: {_team.Score}";
        }
    }
}