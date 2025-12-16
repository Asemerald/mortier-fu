using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using PrimeTween;
using System;

namespace MortierFu
{
    public class PlayerConfirmationUI : MonoBehaviour
    {
        [Header("Player Slots (Blue, Red, Green, Yellow order)")] [SerializeField]
        private List<PlayerSlot> _playerSlots;

        [Header("Animation Settings")] [SerializeField]
        private float _pulseScale = 1.15f;

        [SerializeField] private float _pulseDuration = 0.45f;

        [SerializeField] private float _hideDuration = 0.6f;
        [SerializeField] private float _scaleDuration = 0.5f;

        public void ShowConfirmation(int activePlayerCount)
        {
            StartButtonsAnimation(activePlayerCount);
        }

        public void OnConfirmation()
        {
            HideConfirmation().Forget();
        }

        private async UniTaskVoid HideConfirmation()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.25f));

            foreach (var slot in _playerSlots)
            {
                if (!slot.IsActive) continue;

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Complete();

                slot.ScaleTween = Tween
                    .Scale(slot.AnimatorTransform, Vector3.one, Vector3.zero, _hideDuration, Ease.InQuint)
                    .OnComplete(() => { slot.Animator.enabled = false; });
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_hideDuration));
            
            gameObject.SetActive(false);
        }

        private void StartButtonsAnimation(int playerCount)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                var slot = _playerSlots[i];
                slot.IsActive = i < playerCount;

                slot.Animator.gameObject.SetActive(slot.IsActive);
                slot.AButtonImage.gameObject.SetActive(slot.IsActive);
                slot.OkImage.gameObject.SetActive(false);

                if (slot.ATween.isAlive)
                    slot.ATween.Stop();
                if (slot.ScaleTween.isAlive)
                    slot.ScaleTween.Complete();

                if (!slot.IsActive) continue;

                slot.ScaleTween = Tween.Scale(slot.AnimatorTransform, Vector3.zero, Vector3.one, _scaleDuration,
                    Ease.OutBack);

                slot.ATween = Tween.Scale(
                    target: slot.AButtonImage.rectTransform,
                    Vector3.one * _pulseScale,
                    duration: _pulseDuration,
                    ease: Ease.InOutQuad,
                    cycles: -1,
                    cycleMode: CycleMode.Yoyo
                );
            }
        }

        public void NotifyPlayerConfirmed(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= _playerSlots.Count)
                return;

            var slot = _playerSlots[playerIndex];

            if (slot.ATween.isAlive)
                slot.ATween.Stop();

            slot.AButtonImage.gameObject.SetActive(false);
            slot.Animator.enabled = true;
        }

        [Serializable]
        public class PlayerSlot
        {
            public Image AButtonImage;

            public Image OkImage;

            public Animator Animator;
            public Transform AnimatorTransform;

            public bool IsActive;

            public Tween ATween;
            public Tween ScaleTween;
        }
    }
}