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
        
        private GameModeBase _gameModeBase;
        private Transform[] _playerCards;

        [Header("Settings")] 
        [SerializeField] private bool bullyPossesCard = true;
        [SerializeField] private bool bullyPossesIndicator = false;
        [SerializeField] private bool cardDisplay = true;
        
        private Action _requestSkip;
        #endregion

        #region Unity LifeCycle

        private void OnEnable()
        {
            _skipCts?.Cancel();
            _skipCts?.Dispose();
            
            _skipCts = new CancellationTokenSource();
            _gameModeBase = GameService.CurrentGameMode as GameModeBase;
            _confirmationService = ServiceManager.Instance.Get<ConfirmationService>();
        }

        private void OnDisable()
        {
            CancelAnimations();
            
            EndSkipConfirmation();
        }

        private void OnDestroy()
        {
            EndSkipConfirmation();
            
            CancelAnimations();
            
            _skipCts?.Dispose();
            _skipCts = null;

            _skipFillMaterialInstance = null;
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
                    int newCount = stacks[existingIndex].Count + 1;
                    stacks.RemoveAt(existingIndex);
                    stacks.Add(new AugmentStack(augment, newCount));
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
                cardObj.EnableIndicatorCard(cardObj.transform.childCount > 0,_skipCts.Token);
            }
            else
            {
                cardObj.EnableIndicatorCard(
                    _gameModeBase.GetWinnerPlayerIndex() != playerIndex && cardObj.transform.childCount > 0,
                    _skipCts.Token);
            }
        }

        private void InitializeSkipUI()
        {
            if (!skipFillImage)
            {
                Logs.LogError("No SkippFillImage was found on AugmentSummaryUI");
                return;
            }
            
            if (!skipFillImage.material)
            {
                Logs.LogError("No Material was found on SkipFillImage of AugmentSummaryUI");
                return;
            }
            
            _skipFillMaterialInstance = new Material(skipFillImage.material);

            skipFillImage.material = _skipFillMaterialInstance;
        }
        
        #endregion

        #region Core Logic
        
        private CancellationTokenSource _skipCts;

        public async UniTask AnimatePlayerImagesWithAugments(
            List<List<SO_Augment>> playerAugments,
            UniTask canHideTask,
            Action requestSkip,
            CancellationToken externalCancellationToken)
        {
            _requestSkip = requestSkip;
            
            var ownCts = _skipCts ??= new CancellationTokenSource();
            
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_skipCts.Token, externalCancellationToken);
            
            _background.SetActive(true);

            InitializeSkipUI();
            
            BeginSkipConfirmation();

            try
            {
                int playerCount = playerAugments.Count;

                _playerImages = new Image[playerCount];
                _playerCards = new Transform[playerCount];

                List<List<AugmentStack>> playerAugmentStacks = playerAugments
                    .Select(BuildAugmentStacks)
                    .ToList();

                InitializePlayers(playerAugmentStacks, playerCount);

                await AnimateSummaryUI(playerCount, _skipCts.Token , playerAugmentStacks);

                await HandleAnimationLastAugment(playerCount, _skipCts.Token , canHideTask);
            }
            catch (OperationCanceledException)
            {
                
            }
            finally
            {
                if (_skipCts == ownCts)
                {
                    _skipCts.Dispose();
                    _skipCts = null;
                }
                else
                {
                    ownCts.Dispose();
                }
    
                EndSkipConfirmation();
                CleanAugmentSummaryForNextRound();
                _background.SetActive(false);
                _requestSkip = null;
            }
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
            
            while (canHideTask.Status == UniTaskStatus.Pending && !ct.IsCancellationRequested)
            {
                await UniTask.Yield();
            }
            
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
                    float angle = _settings.AngleOffsetDeparture - i * angleStep;
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
            
            _skipCts?.Cancel();
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

        #region Skip Summary

        [SerializeField] private Image skipFillImage;
        
        private Material _skipFillMaterialInstance;

        private float _currentSkippFillValue;
        
        private static readonly int FillAmount = Shader.PropertyToID("_fillAmount");

        private ConfirmationService _confirmationService;
        
        private bool _isSubscribedToSkipConfirmation;

        private void BeginSkipConfirmation()
        {
            if (_isSubscribedToSkipConfirmation)
                return;

            List<PlayerManager> players = _confirmationService.GetAvailablePlayers();

            _confirmationService.BeginConfirmation(players);

            _currentSkippFillValue = 0f;
            skipFillImage.materialForRendering.SetFloat(FillAmount, _currentSkippFillValue);

            _confirmationService.OnPlayerConfirmed += HandlePlayerConfirm;

            _isSubscribedToSkipConfirmation = true;
        }

        private void EndSkipConfirmation()
        {
            if (!_isSubscribedToSkipConfirmation)
                return;

            _confirmationService.OnPlayerConfirmed -= HandlePlayerConfirm;
            _confirmationService.ResetRuntimeState();
            
            _isSubscribedToSkipConfirmation = false;
        }

        private void HandlePlayerConfirm(int playerIndex)
        {
            PlayerConfirmAsync(_skipCts.Token).Forget();
        }

        private async UniTask PlayerConfirmAsync(CancellationToken ct)
        {
            float target = GetCurrentPlayersPercentageReady();
            float startValue = _currentSkippFillValue;
            const float duration = 0.15f;
            float elapsedTime = 0f;

            try
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    _currentSkippFillValue = Mathf.Lerp(startValue, target, elapsedTime / duration);
                    skipFillImage.materialForRendering.SetFloat(FillAmount, _currentSkippFillValue);
                    
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
            catch (OperationCanceledException) { }
            
            finally
            {
                skipFillImage.materialForRendering.SetFloat(FillAmount, _currentSkippFillValue);

                if (_currentSkippFillValue >= 0.99f)
                {
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
                    EndSkipConfirmation();
                    _skipCts?.Cancel();
                    _requestSkip?.Invoke();
                } 
            }
        }

        private float GetCurrentPlayersPercentageReady()
        {
            if (_confirmationService == null || _confirmationService.PlayersParticipantsCount <= 0)
                return 1f;

            int confirmedCount = _confirmationService.PlayersParticipantsCount - _confirmationService.PendingPlayersCount;
            return Mathf.Clamp01((float)confirmedCount / _confirmationService.PlayersParticipantsCount);
        }

        #endregion
        
    }
}