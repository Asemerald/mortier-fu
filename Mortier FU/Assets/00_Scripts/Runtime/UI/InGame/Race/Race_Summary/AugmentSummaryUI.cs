using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using TMPro;
using UnityEngine.Serialization;

namespace MortierFu
{
    public class AugmentSummaryUI : MonoBehaviour
    {
        #region Variables
        private Image[] _playerImages;
        
        [Header("References")]
        [SerializeField, Required] private SO_RaritySpritesFactory _raritySpritesFactory;
        [SerializeField, Required] private SO_AugmentSummaryUISettings _settings;
        
        [SerializeField] private RectTransform _layoutRectTAugments;
        [SerializeField] private RectTransform _layoutRectTCard;
        
        [SerializeField] private GameObject _background;
        
        private readonly List<Tween> _activeTweens = new();
        
        private CancellationTokenSource _cts;
        private GameModeBase _gameModeBase;
        private Transform[] _playerCards;

        [Header("Settings")] 
        [SerializeField] private bool bullyPossesCard = true;
        [SerializeField] private bool bullyPossesIndicator = false;
        [SerializeField] private bool cardDisplay = true;
        
        #endregion

        #region Unity LifeCycle

        private void OnEnable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _gameModeBase = GameService.CurrentGameMode as GameModeBase;
        }

        private void OnDisable()
        {
            CancelAnimations();
        }

        private void OnDestroy()
        {
            CancelAnimations();
            _cts?.Dispose();
            _cts = null;
        }

        #endregion
        
        #region Stacking

        private List<AugmentStack> BuildAugmentStacks(List<SO_Augment> augments)
        {
            var stacks = new List<AugmentStack>();

            foreach (SO_Augment augment in augments)
            {
                int existingIndex = stacks.FindIndex(stack => stack.Augment == augment);

                if (existingIndex >= 0)
                {
                    AugmentStack existing = stacks[existingIndex];
                    existing.Count++;
                    stacks[existingIndex] = existing;
                }
                else
                {
                    stacks.Add(new AugmentStack(augment, 1));
                }
            }

            return stacks;
        }

        #endregion

        #region Initialization

        private void InitializePlayers(List<List<AugmentStack>> playerAugmentStacks, int playerCount)
        {
            for (int i = 0; i < playerCount; i++)
            {
                Image playerImageRef = InitializePlayer(i,playerAugmentStacks[i]);

                if (playerImageRef)
                {
                    _playerImages[i] = playerImageRef;
                    playerImageRef.sprite = _settings.GetPlayerIconByPlayerIndex(i); 
                }
            }
        }

        private Image InitializePlayer(int playerIndex, List<AugmentStack> augmentStacks)
        {
            Transform playerIcon = Instantiate(_settings.PlayerImage, _layoutRectTAugments).transform;
            playerIcon.localScale = Vector3.zero;
            InitializeAugments(playerIndex, playerIcon, augmentStacks);
            return playerIcon.GetComponent<Image>();
        }

        private void InitializeAugments(int playerIndex, Transform playerTransform, List<AugmentStack> augmentStacks)
        {
            for (int i = 0; i < _settings.RarityIconCount; i++)
                InitializeAugment(playerIndex, i, augmentStacks, playerTransform);
        }

        private void InitializeAugment(int playerIndex, int slotIndex, List<AugmentStack> augmentStacks, Transform playerTransform)
        {
            Transform augmentsIcon = Instantiate(_settings.RarityIcon, playerTransform).transform;
            augmentsIcon.localScale = Vector3.zero;
            augmentsIcon.localPosition = Vector3.zero;
            
            int stackIndex = augmentStacks.Count - 1 - slotIndex; 
            
            if (stackIndex < 0 || stackIndex >= augmentStacks.Count)
            {
                augmentsIcon.gameObject.SetActive(false);
                return;
            }
            
            AugmentStack augmentStack = augmentStacks[stackIndex];

            AugmentIconData iconData = augmentsIcon.gameObject.AddComponent<AugmentIconData>();
            iconData.Augment = augmentStack.Augment;
            iconData.StackCount = augmentStack.Count;
            
            if (slotIndex == 0)
            {
                LastAugmentAnimation lastAugmentAnimation = augmentsIcon.gameObject.AddComponent<LastAugmentAnimation>();
                lastAugmentAnimation.enabled = false;
                
                InitializeCard(playerIndex, augmentStack);
            }

            Image rarityImage = augmentsIcon.GetComponent<Image>();

            if (rarityImage)
            {
                E_AugmentRarity rarity = augmentStack.Augment.Rarity;
                rarityImage.sprite = _raritySpritesFactory.GetRarityBgSpriteFromRarity(rarity);
            }

            if (augmentsIcon.childCount > 0)
            {
                var logoImage = augmentsIcon.GetChild(0).GetComponent<Image>();

                if (logoImage)
                    logoImage.sprite = augmentStack.Augment.SmallSprite;
            }

            if (augmentsIcon.childCount > 1)
            {
                Transform stackTransform = augmentsIcon.GetChild(1);

                if (augmentStack.Count <= 1)
                {
                    stackTransform.gameObject.SetActive(false);
                }
                else
                {
                    TMP_Text stackingText = stackTransform.GetChild(0).GetComponent<TMP_Text>();
                    if (stackingText)
                        stackingText.text = augmentStack.Count.ToString();
                }
            }
        }

        private void InitializeCard(int playerIndex, AugmentStack augmentStack)
        {
            AugmentCardSummaryRaceUI cardObj = Instantiate(_settings.Card, _layoutRectTCard);
            
            cardObj.transform.localScale = Vector3.zero;
            
            cardObj.Initialize();
            
            cardObj.SetAugmentVisual(augmentStack.Augment);
            
            _playerCards[playerIndex] = cardObj.transform;

            if (bullyPossesIndicator)
            {
                cardObj.EnableIndicatorCard(cardObj.transform.childCount > 0,_cts.Token);
            }
            else
            {
                cardObj.EnableIndicatorCard(
                    _gameModeBase.GetWinnerPlayerIndex() != playerIndex && cardObj.transform.childCount > 0,
                    _cts.Token);
            }
        }
        
        #endregion

        #region Core Logic
        
        public async UniTask AnimatePlayerImagesWithAugments(List<List<SO_Augment>> playerAugments, UniTask canHideTask, CancellationToken externalCancellationToken)
        {
            _cts ??= new CancellationTokenSource();

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _cts.Token,
                externalCancellationToken
            );

            var ct = linkedCancellation.Token;

            _background.SetActive(true);

            int playerCount = playerAugments.Count;
            
            _playerImages = new Image[playerCount];
            _playerCards = new Transform[playerCount];

            List<List<AugmentStack>> playerAugmentStacks = playerAugments
                .Select(BuildAugmentStacks)
                .ToList();
            
            InitializePlayers(playerAugmentStacks, playerCount);
            
            await AnimateSummaryUI(playerCount, ct, playerAugmentStacks);
            
            await HandleAnimationLastAugment(playerCount, ct, canHideTask);
            
            CleanAugmentSummaryForNextRound();
            
            _background.SetActive(false);
        }

        private async UniTask AnimateSummaryUI(int playerCount, CancellationToken ct, List<List<AugmentStack>> playerAugmentStacks)
        {
            for (int i = 0; i < playerCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                
                Transform playerTransform = _playerImages[i].transform;
                
                Tween playerTween = Tween.Scale(
                    playerTransform,
                    Vector3.zero,
                    Vector3.one * _settings.PlayerTargetScale,
                    _settings.PlayerScaleDuration,
                    _settings.PlayerScaleEase
                );
                
                _activeTweens.Add(playerTween);
                
                await UniTask.Yield();
                
                AnimateAugmentIcons(playerTransform, ct, playerAugmentStacks[i]).Forget();
                
                await UniTask.Yield();
                
                if (!bullyPossesCard && _gameModeBase.GetWinnerPlayerIndex() == i)
                    continue;
                
                await AnimateCard(_playerCards[i],ct);
                
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_settings.PlayerAnimDelay),
                    cancellationToken: ct
                );
            }
            
            var tweensSnapshot = _activeTweens.ToArray();

            foreach (var tween in tweensSnapshot)
            {
                ct.ThrowIfCancellationRequested();

                if (tween.isAlive)
                    await tween.ToUniTask(cancellationToken: ct);
            }
        }

        private async UniTask HandleAnimationLastAugment(int playerCount, CancellationToken ct, UniTask canHideTask)
        {
            var runningBreathingAnimations = new List<LastAugmentAnimation>();

            for (int i = 0; i < playerCount; i++)
            {
                Transform playerIcon = _playerImages[i].transform;
                Transform lastAugment = playerIcon.GetChild(0);

                if (_gameModeBase.GetWinnerPlayerIndex() == i) 
                    continue;
                
                if (lastAugment.gameObject.activeSelf &&
                    lastAugment.TryGetComponent(out LastAugmentAnimation breathingAnim))
                {
                    breathingAnim.enabled = true;
                    runningBreathingAnimations.Add(breathingAnim);
                }
            }

            await canHideTask;

            ct.ThrowIfCancellationRequested();

            foreach (var breathingAnim in runningBreathingAnimations)
            {
                if (breathingAnim)
                    breathingAnim.enabled = false;
            }

            runningBreathingAnimations.Clear();
        }

        private async UniTask AnimateAugmentIcons(Transform parent, CancellationToken ct, List<AugmentStack> playerAugmentStacks) 
        {
            try
            {
                int augmentsCount = playerAugmentStacks.Count;
                int childCount = parent.childCount;
                
                if (childCount == 0) return;
                
                Vector3[] finalPositions = new Vector3[childCount];
                float angleStep = 360f / childCount;

                for (int i = 0; i < childCount; i++)
                {
                    float angle = -i * angleStep;
                    float rad = angle * Mathf.Deg2Rad;
                    finalPositions[i] = new Vector3(
                        Mathf.Cos(rad) * _settings.AugmentIconRadius,
                        Mathf.Sin(rad) * _settings.AugmentIconRadius,
                        0f
                    );
                }

                float minDelay = 0.05f;
                float maxDelay = _settings.ChildAnimDelay;

                for (int i = 0; i < childCount; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    Transform child = parent.GetChild(i);

                    if (i >= augmentsCount) child.gameObject.SetActive(false);
                    
                    if (!child.gameObject.activeSelf) continue;

                    var scaleTween = Tween.Scale(child, Vector3.zero, Vector3.one, _settings.AugmentIconAnimDuration,
                        _settings.AugmentIconScaleEase);
                    var moveTween = Tween.LocalPosition(child, Vector3.zero, finalPositions[i], _settings.AugmentIconAnimDuration,
                        _settings.AugmentIconMoveEase);

                    _activeTweens.Add(scaleTween);
                    _activeTweens.Add(moveTween);

                    float t = (float)i / (childCount - 1);
                    float delay = Mathf.Lerp(maxDelay, minDelay, Mathf.Pow(t, _settings.ChildAnimationExponentFactor));

                    if (i < childCount - 1)
                        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async UniTask AnimateCard(Transform card, CancellationToken ct)
        {
            if (!card || !cardDisplay) return;
            
            Tween cardTween = Tween.Scale(
                card,
                Vector3.zero,
                Vector3.one * _settings.CardScaleMultiplier,
                _settings.CardDurationScale,
                _settings.CardScaleEase);
            
            await cardTween.ToUniTask(cancellationToken: ct);
        }
        
        
        #endregion

        #region Clean

        private void CancelAnimations()
        {
            foreach (var tween in _activeTweens)
                tween.Stop();
            _activeTweens.Clear();
            
            _cts?.Cancel();
        }
        
        
        private void CleanAugmentSummaryForNextRound() 
        {
            List<Transform> childrenLayoutCardElement  = _layoutRectTCard.Cast<Transform>().ToList(); 
            List<Transform> childrenLayoutAugmentElement  = _layoutRectTAugments.Cast<Transform>().ToList();

            List<Transform> allElementToClean = new List<Transform>();
            
            allElementToClean.AddRange(childrenLayoutCardElement);
            allElementToClean.AddRange(childrenLayoutAugmentElement);
            
            int childCount = allElementToClean.Count;
            
            for (int i = 0; i < childCount; i++)
                Destroy(allElementToClean[i].gameObject);
        }

        #endregion
        
    }
}