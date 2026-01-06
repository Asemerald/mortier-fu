using System;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace MortierFu
{
    public class NewAugmentPickup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameTxt;
        [SerializeField] private TextMeshProUGUI _descTxt;
        [SerializeField] private Image _iconImg;
        [SerializeField] private Image _augmentBack;
        [SerializeField] private Image _rarityBg;
        [SerializeField] private RarityData[] _rarityData;

        [SerializeField] private GameObject _newAugmentIndicator;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private ParticleSystem _augmentParticle;

        [SerializeField] private RectTransform _infoRoot;
        private FaceCamera _faceCamera;
        private int _index;

        private AugmentSelectionSystem _system;

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody == null) return;

            if (other.attachedRigidbody.TryGetComponent(out PlayerCharacter character))
            {
                bool success = _system.NotifyPlayerInteraction(character, _index);
                if (success == false) return;

                Hide();
            }
        }

        public void Initialize(AugmentSelectionSystem system, int augmentIndex)
        {
            _system = system;
            _index = augmentIndex;

            _faceCamera = GetComponent<FaceCamera>();
        }

        public void SetAugmentVisual(SO_Augment augment)
        {
            if (augment == null)
            {
                throw new ArgumentNullException(nameof(augment));
            }

            var data = GetRarityData(augment.Rarity);

            _augmentBack.gameObject.SetActive(false);
            _newAugmentIndicator.SetActive(false);
            _augmentParticle.textureSheetAnimation.SetSprite(0, augment.BackIcon);

            _rarityBg.sprite = data.BgSprite;
            _iconImg.sprite = augment.Icon;
            _nameTxt.SetText(augment.Name.ToUpper());
            _nameTxt.color = data.NameColor;
            _descTxt.SetText(augment.Description);
            _augmentBack.sprite = augment.BackIcon;
        }

        private RarityData GetRarityData(E_AugmentRarity augmentRarity) =>
            Array.Find(_rarityData, data => data.Rarity == augmentRarity);

        public void SetFaceCameraEnabled(bool enable) => _faceCamera.enabled = enable;

        private async UniTask HideInfoUI(float duration = 0.4f)
        {
            Vector2 startPos = _infoRoot.anchoredPosition;
            Vector2 targetPos = startPos + Vector2.down * 5000f;

            await Tween.UIAnchoredPosition(
                _infoRoot,
                targetPos,
                duration,
                Ease.InQuad
            );

            _rarityBg.gameObject.SetActive(false);
        }

        private async UniTask PlayBoonDropTransition()
        {
            SetFaceCameraEnabled(false);

            await Tween.Alpha(
                _canvas.GetComponent<CanvasGroup>(),
                0f,
                0.25f
            );

            _newAugmentIndicator.SetActive(true);
            _augmentParticle.Play();

            Transform back = _augmentBack.transform;
            back.localScale = Vector3.one;

            await Tween.Scale(
                back,
                new Vector3(0.85f, 1.15f, 0.85f),
                0.15f,
                Ease.OutQuad
            );

            await Tween.Scale(
                back,
                Vector3.one,
                0.25f,
                Ease.OutElastic
            );
        }

        public void DisableObjects()
        {
            _nameTxt.gameObject.SetActive(false);
            _descTxt.gameObject.SetActive(false);
            _iconImg.gameObject.SetActive(false);
            
            _augmentBack.gameObject.SetActive(true);
        }

        public async UniTask PlayRevealSequence()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.4f));

            await HideInfoUI();

            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));

            await PlayBoonDropTransition();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        [Serializable]
        private struct RarityData
        {
            public E_AugmentRarity Rarity;
            public Sprite BgSprite;
            public Color NameColor;
        }
    }
}