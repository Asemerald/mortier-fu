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

        [SerializeField] private float _hideInfoDuration = 0.3f;
        [SerializeField] private float _fadeOutDuration = 0.2f;

        [SerializeField] private Ease _slideOutEase = Ease.InQuad;

        [SerializeField] private GameObject _explosionCardVFXPrefab;

        [SerializeField] private float _showExplosionDelay = 0.1f;
        [SerializeField] private float _hideInfoDelay = 0.2f;

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
            _augmentBack.gameObject.SetActive(false);
            _explosionCardVFXPrefab.SetActive(false);

            _nameTxt.SetText(augment.Name.ToUpper());
            _nameTxt.color = data.NameColor;
            _descTxt.SetText(augment.Description);
            _augmentIcon.sprite = augment.SmallSprite;
            _augmentCard.sprite = augment.CardSprite;
        }

        private RarityData GetRarityData(E_AugmentRarity augmentRarity) =>
            Array.Find(_rarityData, data => data.Rarity == augmentRarity);

        public void SetFaceCameraEnabled(bool enable) => _faceCamera.enabled = enable;

        private async UniTask PlayBoonDropTransition(GameObject pickupVFX)
        {
            SetFaceCameraEnabled(false);

            _augmentIcon.transform.localScale = Vector3.one;

            await UniTask.Delay(TimeSpan.FromSeconds(_showExplosionDelay));

            _explosionCardVFXPrefab.SetActive(true);

            await UniTask.Delay(TimeSpan.FromSeconds(_hideInfoDelay));
            DisableObjects();
            pickupVFX.SetActive(true);
        }

        public void DisableObjectsOnFlip()
        {
            _nameTxt.gameObject.SetActive(false);
            _descTxt.gameObject.SetActive(false);

            _augmentBack.gameObject.SetActive(true);
            _augmentIcon.gameObject.SetActive(true);
        }

        private void DisableObjects()
        {
            _nameTxt.gameObject.SetActive(false);
            _descTxt.gameObject.SetActive(false);

            _augmentBack.gameObject.SetActive(false);
            _augmentIcon.gameObject.SetActive(false);
            _augmentCard.gameObject.SetActive(false);
        }

        public async UniTask PlayRevealSequence(GameObject pickupVFX)
        {
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
            _augmentCard.gameObject.SetActive(true);

            _augmentBack.gameObject.SetActive(false);
            _augmentIcon.gameObject.SetActive(false);

            SetFaceCameraEnabled(true);
        }

        public void Reset()
        {
            _explosionCardVFXPrefab.SetActive(false);
        }

        [Serializable]
        private struct RarityData
        {
            public E_AugmentRarity Rarity;
            public Color NameColor;
        }
    }
}