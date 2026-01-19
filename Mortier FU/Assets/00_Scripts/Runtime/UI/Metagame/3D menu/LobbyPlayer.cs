using TMPro;
using UnityEngine;

namespace MortierFu
{
    public class LobbyPlayer : MonoBehaviour
    {
        [Header("Skins")]
        [SerializeField] private GameObject[] availableSkins;
        [SerializeField] private GameObject[] skinsOutline;
        
        [Header("Faces")]
        [SerializeField] private SkinnedMeshRenderer faceMeshRenderer;
        [SerializeField] private string columnPropertyName = "_Column";
        [SerializeField] private string rowPropertyName = "_Row";
        [SerializeField] private int maxColumns = 4;
        [SerializeField] private int maxRows = 3;
        
        [Header("UI")]
        [SerializeField] private GameObject readyIndicator;
        [SerializeField] private GameObject[] skinSelectionIndicator; // pour montrer qu'on est en mode skin
        [SerializeField] private GameObject[] faceSelectionIndicator; // pour montrer qu'on est en mode face
        
        private int currentSkinIndex = 0;
        private int currentColumn = 1; // Entre 1 et 4
        private int currentRow = 1;    // Entre 1 et 3
        private bool isReady = false;
        private bool isSelectingFace = false; // false = skin, true = face
    
        public bool IsReady => isReady;
        public int SkinIndex => currentSkinIndex;
        public int FaceColumn => currentColumn;
        public int FaceRow => currentRow;

        private Animator _animator;
        private Material _faceMaterial;
    
        private void Awake()
        {
            // Désactiver tous les skins au départ sauf le premier
            for (int i = 0; i < availableSkins.Length; i++)
            {
                availableSkins[i].SetActive(i == 0);
                skinsOutline[i].SetActive(i == 0);
            }
            
            _animator = GetComponentInChildren<Animator>();
            
            // Récupérer le material de la face
            if (faceMeshRenderer != null)
            {
                _faceMaterial = faceMeshRenderer.material;
                UpdateFaceShader();
            }
        }
    
        public void Start()
        {
            currentSkinIndex = 0;
            currentColumn = 1;
            currentRow = 1;
            isReady = false;
            isSelectingFace = false;
            UpdateVisuals();
        }
    
        public void ChangeSkin(Vector2 input)
        {
            if (isReady) return;
    
            // Navigation verticale : changer de mode (skin/face)
            if (input.y < -0.5f && !isSelectingFace)
            {
                // Passer en mode sélection de visage
                isSelectingFace = true;
                UpdateSelectionIndicators();
                return;
            }
            if (input.y > 0.5f && isSelectingFace)
            {
                // Revenir en mode sélection de skin
                isSelectingFace = false;
                UpdateSelectionIndicators();
                return;
            }
    
            // Navigation horizontale : changer skin ou face selon le mode
            if (!isSelectingFace)
            {
                // Mode sélection de skin
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
            else
            {
                // Mode sélection de visage - cycle à travers toutes les combinaisons
                if (input.x > 0.5f)
                {
                    // Aller au visage suivant
                    currentColumn++;
                    if (currentColumn > maxColumns)
                    {
                        currentColumn = 1;
                        currentRow++;
                        if (currentRow > maxRows)
                        {
                            currentRow = 1;
                        }
                    }
                    UpdateFaceShader();
                }
                else if (input.x < -0.5f)
                {
                    // Aller au visage précédent
                    currentColumn--;
                    if (currentColumn < 1)
                    {
                        currentColumn = maxColumns;
                        currentRow--;
                        if (currentRow < 1)
                        {
                            currentRow = maxRows;
                        }
                    }
                    UpdateFaceShader();
                }
            }
        }
    
        private void UpdateFaceShader()
        {
            if (_faceMaterial != null)
            {
                _faceMaterial.SetFloat(columnPropertyName, currentColumn);
                _faceMaterial.SetFloat(rowPropertyName, currentRow);
            }
        }
        
        private void UpdateSelectionIndicators()
        {
            if (skinSelectionIndicator != null)
                skinSelectionIndicator.SetActive(!isSelectingFace);
                
            if (faceSelectionIndicator != null)
                faceSelectionIndicator.SetActive(isSelectingFace);
        }
    
        public void ToggleReady()
        {
            isReady = !isReady;
            UpdateVisuals();
            MenuManager.Instance?.CheckAllPlayersReady();
            
            if (_animator != null)
            {
                _animator.SetBool("bIsReady", isReady);
                
                if (isReady)
                    _animator.SetTrigger("ReadyTrigger");
            }
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
                skinsOutline[i].SetActive(i == currentSkinIndex);
            }
        }
    
        private void UpdateVisuals()
        {
            UpdateSkinDisplay();
            UpdateFaceShader();
            UpdateSelectionIndicators();
        
            if (readyIndicator != null)
            {
                //readyIndicator.SetActive(isReady);
            }
        }
    }
}