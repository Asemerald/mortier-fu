using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace MortierFu
{
    public sealed class AugmentCardSummaryRaceUI : AugmentCardUI
    {
        [SerializeField] private RectTransform indicatorPickCard;
        [SerializeField] private float yOffset = 15f;
        
        public void EnableIndicatorCard(bool activeIndicatorCard,CancellationToken ct)
        {
            indicatorPickCard.gameObject.SetActive(activeIndicatorCard);
            ActivateIndicatorCardAnimation(ct);
            if(_vfxCard)
                _vfxCard.transform.localScale = Vector3.one * 15f;
        }
        
        private void ActivateIndicatorCardAnimation(CancellationToken ct)
        {
            if (!indicatorPickCard.gameObject.activeInHierarchy) return;
            
            float currentIndicatorYPos = indicatorPickCard.localPosition.y;

            var indicatorTween = Tween.LocalPositionY(
                indicatorPickCard,
                currentIndicatorYPos - yOffset,
                0.5f,
                Ease.OutQuad,
                cycles: -1,
                CycleMode.Yoyo
            ).ToUniTask(PlayerLoopTiming.Update,ct);
        }
    }
}

