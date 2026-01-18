using TMPro;
using UnityEngine;

namespace MortierFu
{
    public class LobbyPlayer : MonoBehaviour
    {
        [SerializeField] private GameObject[] availableSkins;
        [SerializeField] private GameObject readyIndicator;
        [SerializeField] private TextMeshProUGUI playerNumberText;
    
        private PlayerManager _playerManager;
        private int currentSkinIndex = 0;
        private bool isReady = false;
    
        public bool IsReady => isReady;
        public int SkinIndex => currentSkinIndex;
    
        private void Awake()
        {
            // Désactiver tous les skins au départ sauf le premier
            for (int i = 0; i < availableSkins.Length; i++)
            {
                availableSkins[i].SetActive(i == 0);
            }
        }
    
        public void Initialize(PlayerManager playerManager)
        {
            _playerManager = playerManager;
            currentSkinIndex = 0; // Reset au skin par défaut
            isReady = false;
            UpdateVisuals();
        }
    
        public void ChangeSkin(Vector2 input)
        {
            if (isReady) return;
        
            if (input.x > 0.5f)
            {
                currentSkinIndex = (currentSkinIndex + 1) % availableSkins.Length;
                UpdateSkinDisplay();
            }
            else if (input.x < -0.5f)
            {
                currentSkinIndex--;
                if (currentSkinIndex < 0) currentSkinIndex = availableSkins.Length - 1;
                UpdateSkinDisplay();
            }
        }
    
        public void ToggleReady()
        {
            isReady = !isReady;
            UpdateVisuals();
            MenuManager.Instance?.CheckAllPlayersReady();
        }
    
        public void Unready()
        {
            if (isReady)
            {
                isReady = false;
                UpdateVisuals();
            }
        }
    
        private void UpdateSkinDisplay()
        {
            for (int i = 0; i < availableSkins.Length; i++)
            {
                availableSkins[i].SetActive(i == currentSkinIndex);
            }
        }
    
        private void UpdateVisuals()
        {
            UpdateSkinDisplay();
        
            if (readyIndicator != null)
            {
                readyIndicator.SetActive(isReady);
            }
        }
    }
}