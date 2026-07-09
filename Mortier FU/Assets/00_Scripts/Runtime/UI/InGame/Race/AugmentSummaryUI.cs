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

namespace MortierFu
{
    public class AugmentSummaryUI : MonoBehaviour
    {
        #region Variables
        private readonly Image[] _playerImages = new Image[3];

        [Header("References")]
        [SerializeField, Required] private SO_RaritySpritesFactory _soRaritySpritesFactory;
        [SerializeField, Required] private SO_AugmentSummaryUISettings _settings;
        
        [SerializeField] private RectTransform layoutRectT;
        [SerializeField] private GameObject _background;
        
        private readonly List<Tween> _activeTweens = new();
        private CancellationTokenSource _cts;
        private GameModeBase _gameModeBase;

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
        
        private void CancelAnimations()
        {
            foreach (var tween in _activeTweens)
                tween.Stop();

            _activeTweens.Clear();

            _cts?.Cancel();
        }

        #region Initialization

        private void InitializePlayers(List<List<SO_Augment>> playerAugments, int players)
        {
            for (int i = 0; i < players; i++)
            {
                Image playerImageRef = InitializePlayer(playerAugments[i]);

                if (playerImageRef)
                {
                    _playerImages[i] = playerImageRef;
                    playerImageRef.sprite = _settings.GetPlayerIconByPlayerIndex(i); 
                }
            }
        }

        private Image InitializePlayer(List<SO_Augment> augments)
        {
            Transform playerIcon = Instantiate(_settings.PlayerImage, layoutRectT).transform;
            
            playerIcon.localScale = Vector3.zero;
            
            InitializeAugments(playerIcon,augments);
            
            return  playerIcon.GetComponent<Image>();
        }

        private void InitializeAugments(Transform playerTransform, List<SO_Augment> augments)
        {
            for (int i = 0; i < _settings.RarityIconCount; i++)
            {
                InitializeAugment(i,augments, playerTransform);
            }
            
        }

        private void InitializeAugment(int indexAugment, List<SO_Augment> augments, Transform playerTransform)
        {
            Transform augmentsIcon = Instantiate(_settings.RarityIcon, playerTransform).transform;
            augmentsIcon.localScale = Vector3.zero;
            augmentsIcon.localPosition = Vector3.zero;

            if (indexAugment == 0) 
            {
                LastAugmentAnimation lastAugmentAnimation = augmentsIcon.gameObject.AddComponent<LastAugmentAnimation>();
                lastAugmentAnimation.enabled = false;
            }
            
            int augmentIndex = augments.Count - 1 - indexAugment; 
            
            if (augmentIndex < 0 || augmentIndex >= augments.Count)
            {
                augmentsIcon.gameObject.SetActive(false);
                return;
            }

            SO_Augment augment = augments[augmentIndex];

            Image rarityImage = augmentsIcon.GetComponent<Image>();

            if (rarityImage)
            {
                E_AugmentRarity rarity = augment.Rarity;
                rarityImage.sprite = _soRaritySpritesFactory.GetRarityBgSpriteFromRarity(rarity);
            }

            if (augmentsIcon.childCount > 0)
            {
                var logoImage = augmentsIcon.GetChild(0).GetComponent<Image>();

                if (logoImage)
                    logoImage.sprite = augment.SmallSprite;
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
            
            InitializePlayers(playerAugments,playerCount);
            
            await AnimateSummaryUI(playerCount, ct, playerAugments);

            await HandleAnimationLastAugment(playerCount, ct, canHideTask);

            CleanAugmentSummaryForNextRound();
            
            _background.SetActive(false);
        }

        private async UniTask AnimateSummaryUI(int players, CancellationToken ct,List<List<SO_Augment>> playerAugments)
        {
            for (int i = 0; i < players; i++)
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
                
                AnimateChildren(playerTransform, ct, playerAugments[i]).Forget();
                
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

        private async UniTask HandleAnimationLastAugment(int players, CancellationToken ct, UniTask canHideTask)
        {
            var runningBreathingAnimations = new List<LastAugmentAnimation>();

            for (int i = 0; i < players; i++)
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

        private async UniTask AnimateChildren(Transform parent, CancellationToken ct, List<SO_Augment> augmentsPlayer) 
        {
            try
            {
                int augments = augmentsPlayer.Count;
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

                    if (i >= augments) child.gameObject.SetActive(false);
                    
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
        

        private void CleanAugmentSummaryForNextRound() 
        {
            List<Transform> childrenLayoutElement  = layoutRectT.Cast<Transform>().ToList(); 
            
            int childCount = childrenLayoutElement.Count;
            
            for (int i = 0; i < childCount; i++)
                Destroy(childrenLayoutElement[i].gameObject);
        }

        #endregion
        
    }
}