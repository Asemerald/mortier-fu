using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Image[] skinSelectionIndicator; // pour montrer qu'on est en mode skin
        [SerializeField] private Image[] faceSelectionIndicator; // pour montrer qu'on est en mode face
        [SerializeField] private Sprite selectedArrowSprite;
        [SerializeField] private Sprite unselectedArrowSprite;
        [SerializeField] private GameObject[] joinButtonIndicators;
        
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

        private void OnEnable()
        {
            currentSkinIndex = 0;
            currentColumn = 1;
            currentRow = 1;
            isReady = false;
            isSelectingFace = false;
            UpdateVisuals();

            if (joinButtonIndicators != null)
            {
                foreach (var t in joinButtonIndicators)
                {
                    t.SetActive(false);
                }
            }
        }

        private void OnDisable()
        {
            if (joinButtonIndicators != null)
            {
                foreach (var t in joinButtonIndicators)
                {
                    t.SetActive(true);
                }
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
    
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Slider, transform.position);
            
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
        
        private void UpdateSelectionIndicators(bool hide = false)
        {
            foreach (var t in skinSelectionIndicator)
            {
                t.gameObject.SetActive(!hide);
                t.sprite = isSelectingFace ? unselectedArrowSprite : selectedArrowSprite;
                t.gameObject.GetComponentInChildren<Image>().sprite = isSelectingFace ? unselectedArrowSprite : selectedArrowSprite; // pardon pour ça si qqun le voit un jour
            }

            foreach (var t in faceSelectionIndicator)
            {
                t.gameObject.SetActive(!hide);
                t.sprite = isSelectingFace ? selectedArrowSprite : unselectedArrowSprite;
                t.gameObject.GetComponentInChildren<Image>().sprite = isSelectingFace ? selectedArrowSprite : unselectedArrowSprite; // idem hein 
            }
        }
    
        public void ToggleReady()
        {
            if (MenuManager.Instance.AllPlayersReady())
            {
                return;
            }
            
            isReady = !isReady;
            UpdateVisuals();
            MenuManager.Instance?.CheckAllPlayersReady();
            AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Ready);
            
            if (_animator != null)
            {
                _animator.SetBool("bIsReady", isReady);
                
                if (isReady)
                    _animator.SetTrigger("ReadyTrigger");
            }
            
            //Hide selection indicators when ready
            UpdateSelectionIndicators(true);
        }
    
        public void Unready()
        {
            if (isReady)
            {
                isReady = false;
                AudioService.PlayOneShot(AudioService.FMODEvents.SFX_UI_Tick);
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
            UpdateSelectionIndicators(false);
        
            if (readyIndicator != null)
            {
                //readyIndicator.SetActive(isReady);
            }
        }
    }
}