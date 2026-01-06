using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class AugmentCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameTxt;
        [SerializeField] private TextMeshProUGUI _descTxt;
        [SerializeField] private Image _augmentIcon;
        [SerializeField] private Image _augmentCard;
        [SerializeField] private Image _augmentBack;
        [SerializeField] private RarityData[] _rarityData;

        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private RectTransform _infoRoot;
        
        [SerializeField] private float _hideInfoDuration = 0.4f;
        [SerializeField] private float _fadeOutDuration = 0.25f;
        
        [SerializeField] private Ease _slideOutEase = Ease.InQuad;
        
        private FaceCamera _faceCamera;
        
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        private Vector2 _initialInfoPos;
        private float _initialCanvasAlpha;

        private bool _initialized;
        
        public void Initialize()
        {
            _faceCamera = GetComponent<FaceCamera>();

            if (_initialized)
                return;

            _initialRotation = transform.localRotation;
            _initialScale = transform.localScale;
            _initialInfoPos = _infoRoot.anchoredPosition;
            _initialCanvasAlpha = _canvasGroup.alpha;
        }

        public void SetAugmentVisual(SO_Augment augment)
        {
            if (augment == null)
            {
                throw new ArgumentNullException(nameof(augment));
            }

            var data = GetRarityData(augment.Rarity);

            _augmentIcon.gameObject.SetActive(false);
           // _augmentParticle.textureSheetAnimation.SetSprite(0, augment.Icon);
            _augmentBack.gameObject.SetActive(false);
            
            _nameTxt.SetText(augment.Name.ToUpper());
            _nameTxt.color = data.NameColor;
            _descTxt.SetText(augment.Description);
            _augmentIcon.sprite = augment.Icon;
            _augmentCard.sprite = augment.AugmentCardVisual;
        }

        private RarityData GetRarityData(E_AugmentRarity augmentRarity) =>
            Array.Find(_rarityData, data => data.Rarity == augmentRarity);

        public void SetFaceCameraEnabled(bool enable) => _faceCamera.enabled = enable;

        private async UniTask HideInfoUI()
        {
            Vector2 startPos = _infoRoot.anchoredPosition;
            Vector2 targetPos = startPos + Vector2.down * 5000f;

            await Tween.UIAnchoredPosition(
                _infoRoot,
                targetPos,
                _hideInfoDuration,
                _slideOutEase
            );
        }

        private async UniTask PlayBoonDropTransition(GameObject pickupVFX)
        {
            SetFaceCameraEnabled(false);

            await Tween.Alpha(
                _canvasGroup,
                0f,
                _fadeOutDuration
            );

            Transform augmentIcon = _augmentIcon.transform;
            augmentIcon.localScale = Vector3.one;

            await Tween.Scale(
                augmentIcon,
                new Vector3(0.85f, 1.15f, 0.85f),
                0.15f,
                Ease.OutQuad
            );

            await Tween.Scale(
                augmentIcon,
                Vector3.one,
                0.25f,
                Ease.OutElastic
            );
            
            pickupVFX.SetActive(true);
        }

        public void DisableObjects()
        {
            _nameTxt.gameObject.SetActive(false);
            _descTxt.gameObject.SetActive(false);
            
            _augmentBack.gameObject.SetActive(true);
            _augmentIcon.gameObject.SetActive(true);
        }

        public async UniTask PlayRevealSequence(GameObject pickupVFX)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

            await HideInfoUI();

            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            await PlayBoonDropTransition(pickupVFX);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
        
        public void ResetUI()
        {
            transform.localRotation = _initialRotation;
            transform.localScale = _initialScale;

            _canvasGroup.alpha = _initialCanvasAlpha;

            _infoRoot.anchoredPosition = _initialInfoPos;

            _nameTxt.gameObject.SetActive(true);
            _descTxt.gameObject.SetActive(true);

            _augmentBack.gameObject.SetActive(false);
            _augmentIcon.gameObject.SetActive(false);

            SetFaceCameraEnabled(true);
        }

        [Serializable]
        private struct RarityData
        {
            public E_AugmentRarity Rarity;
            public Color NameColor;
        }
    }
}