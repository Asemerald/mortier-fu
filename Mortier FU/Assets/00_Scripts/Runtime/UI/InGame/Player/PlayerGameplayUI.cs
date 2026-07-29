using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MortierFu.Shared;
using PrimeTween;
using System.Threading;

namespace MortierFu
{
    public class PlayerGameplayUI : MonoBehaviour
    {

        #region Variables

        [Header("Params")] 
        
        [SerializeField] private float _healthTweenDuration = 0.18f;
        [SerializeField] private float _damageFbDelay = 0.3f;
        [SerializeField] private float _damageFbDepleteDuration = 0.8f;
        [SerializeField] private float _tweenDuration = 0.5f;
        [SerializeField] private float _startFadeDelay = 2f;

        [SerializeField] private Ease _healthBarEase = Ease.OutBack;
        
        [Header("References")] 
        
        [SerializeField] private PlayerCharacter _character;

        [SerializeField] private Sprite[] backgroundPlayerColor;

        [SerializeField] private Image _playerColorImg;
        [SerializeField] private Image _healthFillImg;
        [SerializeField] private Image _damageFillImg;
        [SerializeField] private Image _healthTicksImg;
        [SerializeField] private Image _reloadCdImage;
        
        [SerializeField] private RectTransform _playerHUD;
        
        [Header("Dash")] 
        [SerializeField] private Image _strikeCdImage;
        [SerializeField] private RawImage _dashChargeTiledImg;
        
        private Tween _damageTween;
        private Tween _healthTween;
        private Tween _hudTween;
        
        private float _damageBarWidth;
        private float _previousDamageNormalized;
        
        private Material _ticksMaterialInstance;

        private int _currentDashCharges = 0;

        private GameModeBase _gm;

        private CancellationTokenSource _hudCancellation;

        private const int k_maxDashCharges = 7;

        private readonly Vector3 _scaleOne = Vector3.one;

        private static readonly int FillID = Shader.PropertyToID("_Fill");
        private static readonly int ScalingYid = Shader.PropertyToID("_ScalingY");
        private static readonly int BlinkFactor = Shader.PropertyToID("_BlinkFactor");

        public bool IsIntroReady { get; private set; }

        public event Action<bool> OnIntroReady;

        #endregion

        #region Unity Lifecycle

        private void Awake() => _gm = GameService.CurrentGameMode as GameModeBase;

        private void OnEnable()
        {
            if (_character == null)
            {
                enabled = false;
                Logs.LogWarning("Missing character reference on PlayerGameplayUI. Falling asleep.", this);
                return;
            }

            if (_gm != null)
            {
                _gm.OnRoundEnded -= OnRoundEnded;
                _gm.OnRaceStart -= OnRaceStart;

                _gm.OnRoundEnded += OnRoundEnded;
                _gm.OnRaceStart += OnRaceStart;
            }

            _ticksMaterialInstance = new Material(_healthTicksImg.material);
            _healthTicksImg.material = _ticksMaterialInstance;

            if (_character.Health != null)
            {
                _character.Health.OnHealthChanged -= OnHealthChanged;
                _character.Health.OnMaxHealthChanged -= OnMaxHealthChanged;

                _character.Health.OnHealthChanged += OnHealthChanged;
                _character.Health.OnMaxHealthChanged += OnMaxHealthChanged;
            }

            if (_character.Stats != null && _character.Stats.DashCharges != null)
            {
                _character.Stats.DashCharges.OnDirtyUpdated -= OnDashChargeUpdated;
                _character.Stats.DashCharges.OnDirtyUpdated += OnDashChargeUpdated;
            }

            Material strikeMat = new(_strikeCdImage.material);
            _strikeCdImage.material = strikeMat;
            Material reloadMat = new(_reloadCdImage.material);
            _reloadCdImage.material = reloadMat;
        }

        private void OnDisable()
        {
            CancelHudAnimation();
            ResetIntroReady();

            if (_character != null)
            {
                if (_character.Health != null)
                {
                    _character.Health.OnHealthChanged -= OnHealthChanged;
                    _character.Health.OnMaxHealthChanged -= OnMaxHealthChanged;
                }

                if (_character.Stats != null && _character.Stats.DashCharges != null)
                    _character.Stats.DashCharges.OnDirtyUpdated -= OnDashChargeUpdated;
            }

            if (_gm != null)
            {
                _gm.OnRoundEnded -= OnRoundEnded;
                _gm.OnRaceStart -= OnRaceStart;
            }

            if (_damageTween.isAlive)
                _damageTween.Stop();

            if (_healthTween.isAlive)
                _healthTween.Stop();
        }

        private void Start()
        {
            StartShowPlayerHUD(showIcon: true);

            OnHealthChanged(0f, 1f);
            OnMaxHealthChanged(1f);

            RectTransform parentRect = _damageFillImg.rectTransform.parent as RectTransform;

            if (parentRect)
                _damageBarWidth = parentRect.rect.width;
        }
        

        private void OnDestroy()
        {
            CancelHudAnimation();

            OnIntroReady = null;

            if (_ticksMaterialInstance == null)
                return;

            Destroy(_ticksMaterialInstance);
            _ticksMaterialInstance = null;
        }

        private void Update()
        {
            UpdateReloadUI();
            UpdateStrikeUI();

            if (_gm?.CurrentGameState is GameState.AugmentRace or GameState.Round or GameState.RoundCountdown)
                ScaleHUDToAvatarSize();
        }

        #endregion

        #region Health

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            float fillAmount = _character.Health.HealthRatio;
            float maxHealth = _character.Health.MaxHealth;
            float oldFillAmount = Mathf.Clamp01(oldHealth / maxHealth);

            if (_healthTween is { isAlive: true, progress: < 1f })
                _healthTween.Stop();

            _healthTween = Tween.Custom(_healthFillImg.fillAmount, fillAmount, _healthTweenDuration, newFillAmount =>
            {
                _healthFillImg.fillAmount = newFillAmount;
                _ticksMaterialInstance.SetFloat(FillID, newFillAmount);
            }, Ease.OutQuad);

            if (newHealth >= oldHealth) return;

            if (_damageTween is { isAlive: true, progress: < 1f })
                _damageTween.Stop();

            _damageFillImg.fillAmount = oldFillAmount;
            _damageTween = Tween.UIFillAmount(_damageFillImg, oldFillAmount, fillAmount, _damageFbDepleteDuration, Ease.InOutQuad, startDelay: _damageFbDelay);
        }

        private void OnMaxHealthChanged(float _)
        {
            float baseHealth = _character.Stats.MaxHealth.BaseValue;
            float maxHealth = _character.Stats.MaxHealth.Value;

            float delta = maxHealth - baseHealth;
            float scalingY = 0.5f + delta / baseHealth * 0.5f;

            _ticksMaterialInstance.SetFloat(ScalingYid, scalingY);

            // Redraw health
            OnHealthChanged(0f, 0f);
        }

        #endregion

        #region Dash

        private void OnDashChargeUpdated() => UpdateDashChargeSprite();

        private void UpdateDashChargeSprite()
        {
            // Retrieve from character the amount of charges.
            int dashCharges = _character.AvailableDashCharges;

            // If we the amount hasn't change, no reason to change the sprite.
            if (_currentDashCharges == dashCharges)
                return;

            _currentDashCharges = dashCharges;
            BlinkUI(_strikeCdImage, 0.07f).Forget();

            if (_character.Stats.DashCharges.Value < 2 || _currentDashCharges == 0)
            {
                _dashChargeTiledImg.enabled = false;
                return;
            }

            _dashChargeTiledImg.enabled = true;

            Rect rect = _dashChargeTiledImg.uvRect;
            rect.x = 1f / k_maxDashCharges * dashCharges;
            _dashChargeTiledImg.uvRect = rect;
        }
        
        private void UpdateStrikeUI()
        {
            // Reverse progress bar
            float strikeProgress = 1 - _character.GetStrikeCooldownProgress;

            _strikeCdImage.fillAmount = strikeProgress;

            _strikeCdImage.color = _character.AvailableDashCharges > 0 ? Color.Lerp(_strikeCdImage.color, Color.white, 25 * Time.deltaTime) : new Color(0.75f, 0.75f, 0.75f, 1);

            UpdateDashChargeSprite();
        }

        #endregion

        #region HUD

        private void StartShowPlayerHUD(bool showIcon)
        {
            CancelHudAnimation();

            _hudCancellation = new CancellationTokenSource();
            
            ShowPlayerHUDAsync(showIcon, _hudCancellation.Token).Forget();
        }

        private async UniTaskVoid ShowPlayerHUDAsync(bool showIcon, CancellationToken cancellationToken)
        {
            try
            {
                PrepareHUDVisuals();
                
                ResetIntroReady();
                
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                
                await UniTask.Delay(TimeSpan.FromSeconds(_startFadeDelay), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);

                await AnimateHUDInAsync(cancellationToken);

                MarkIntroReady();
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                Logs.LogError($"[PlayerGameplayUI] HUD intro failed: {e.Message}", this);
            }
        }

        private async UniTask AnimateHUDInAsync(CancellationToken cancellationToken)
        {
            if (!_playerHUD)
            {
                Logs.LogError("[PlayerGameplayUI] Player HUD reference is missing.", this);
                MarkIntroReady();
                return;
            }

            _playerHUD.localScale = Vector3.zero;

            _hudTween = Tween.Scale(_playerHUD, _scaleOne, _tweenDuration, _healthBarEase, useUnscaledTime: true);

            await WaitForTweenAsync(_hudTween, cancellationToken);
        }

        private void PrepareHUDVisuals()
        {
            if (_playerHUD)
                _playerHUD.localScale = Vector3.zero;

            InitBackgroundColorHUD();
        }
        
        private void InitBackgroundColorHUD()
        {
            int index = _character.Owner ? _character.Owner.PlayerIndex : 0;
            
            _playerColorImg.sprite = backgroundPlayerColor[index];
        }
        
        private void CancelHudAnimation()
        {
            _hudCancellation?.Cancel();
            _hudCancellation?.Dispose();
            _hudCancellation = null;

            if (_hudTween.isAlive)
                _hudTween.Stop();
        }
        
        private async UniTask HidePlayerHUD()
        {
            CancelHudAnimation();

            ResetIntroReady();

            if (!_playerHUD)
                return;

            _hudTween = Tween.Scale(_playerHUD, Vector3.zero, _tweenDuration, _healthBarEase, useUnscaledTime: true);

            await WaitForTweenAsync(_hudTween, CancellationToken.None);
        }
        
        private void ScaleHUDToAvatarSize()
        {
            //TODO : Refacto ce bousin si possible (Eliot)

            var scale = 1 / _character.transform.localScale.x;
            if (scale < 1)
            {
                scale += _character.transform.localScale.x * 0.04f;
            }

            _playerHUD.localScale = new Vector3(scale, scale, scale);

            var yOffset = 1.27f - (Mathf.Abs(1 - _character.transform.localScale.x) * 0.25f);
            _playerHUD.localPosition = new Vector3(0, yOffset, 0);
        }

        #endregion

        #region GameState Handle

        private void OnRaceStart() => StartShowPlayerHUD(showIcon: false);
        
        private void OnRoundEnded(RoundInfo roundInfo) => HidePlayerHUD().Forget();

        #endregion
        
        private void UpdateReloadUI()
        {
            // Reverse progress bar
            float reloadProgress = 1 - _character.Mortar.ShootCooldownProgress;

            _reloadCdImage.fillAmount = reloadProgress;
            _reloadCdImage.color = reloadProgress >= 1f ? Color.Lerp(_reloadCdImage.color, Color.white, 25 * Time.deltaTime) : new Color(0.75f, 0.75f, 0.75f, 1);
        }

        private void ResetIntroReady() => IsIntroReady = false;

        private void MarkIntroReady()
        {
            if (IsIntroReady)
                return;

            IsIntroReady = true;
            OnIntroReady?.Invoke(this);
        }
        
        private static async UniTask WaitForTweenAsync(Tween tween, CancellationToken cancellationToken)
        {
            while (tween.isAlive)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }
        
        private async UniTask BlinkUI(Image sprite, float duration)
        {
            sprite.material.SetFloat(BlinkFactor, 0.9f);
            await UniTask.Delay(TimeSpan.FromSeconds(duration));
            sprite.material.SetFloat(BlinkFactor, 0);
        }

        
    }
}