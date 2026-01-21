using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using MortierFu;
using MortierFu.Shared;
using TMPro;
using PrimeTween;

public class PlayerGameplayUI : MonoBehaviour
{
    [Header("Params")] [SerializeField] private float _healthTweenDuration = 0.2f;
    [SerializeField] private float _damageFbDelay = 0.225f;
    [SerializeField] private float _damageFbDepleteDuration = 0.4f;

    [Header("References")] [SerializeField]
    private PlayerCharacter _character;

    [SerializeField] private Image _healthFillImg;
    [SerializeField] private Image _damageFillImg;
    private Tween _damageTween;
    private Tween _healthTween;
    private float _damageBarWidth;
    private float _previousDamageNormalized;
    [SerializeField] private Image _healthTicksImg;
    private Material _ticksMaterialInstance;
    [SerializeField] private Image _playerHUD;
    [SerializeField] private Image _strikeCdImage;
    [SerializeField] private Image _characterIcon;

    [SerializeField] private Sprite[] _characterIcons;
    [SerializeField] private Sprite[] _playerHUDSprites;

    [SerializeField] private float _tweenDuration = 0.5f;
    [SerializeField] private float _startFadeDelay = 2f;

    [SerializeField] private Ease _iconInEase = Ease.InBack;
    [SerializeField] private Ease _healthBarEase = Ease.OutBack;

    [Header("Dash")] [SerializeField] private GameObject _dashChargeBg;
    [SerializeField] private RawImage _dashChargeTiledImg;
    private int _currentDashCharges = 0;

    private GameModeBase _gm;

    private const int k_maxDashCharges = 7;

    private readonly Vector3 _scaleOne = Vector3.one;

    private static readonly int FillID = Shader.PropertyToID("_Fill");
    private static readonly int ScalingYID = Shader.PropertyToID("_ScalingY");

    private void Awake()
    {
        _gm = GameService.CurrentGameMode as GameModeBase;
    }

    private void OnEnable()
    {
        if (_gm != null)
        {
            _gm.OnRoundEnded += OnRoundEnded;
            _gm.OnRaceStart += OnRaceStart;
        }

        if (_character == null)
        {
            enabled = false;
            Logs.LogWarning("Missing character reference on PlayerGameplayUI. Falling asleep.", this);
            return;
        }

        _ticksMaterialInstance = new Material(_healthTicksImg.material);
        _healthTicksImg.material = _ticksMaterialInstance;

        _character.Health.OnHealthChanged += OnHealthChanged;
        _character.Health.OnMaxHealthChanged += OnMaxHealthChanged;
        _character.Stats.DashCharges.OnDirtyUpdated += OnDashChargeUpdated;
    }

    private void OnDisable()
    {
        if (_character == null) return;

        _character.Health.OnHealthChanged -= OnHealthChanged;
        _character.Stats.DashCharges.OnDirtyUpdated -= OnDashChargeUpdated;
        _gm.OnRoundEnded -= OnRoundEnded;
    }

    private void Start()
    {
        _playerHUD.sprite = _playerHUDSprites[_character.Owner.PlayerIndex];
        ShowPlayerHUD(true).Forget();

        OnHealthChanged(0f, 1f);
        OnMaxHealthChanged(1f);

        // Calculate damage bar width
        var parentRect = _damageFillImg.rectTransform.parent as RectTransform;
        if (parentRect)
        {
            _damageBarWidth = parentRect.rect.width;
        }
    }

    private void Update()
    {
        // Reverse progress bar
        float strikeProgress = 1 - _character.GetStrikeCooldownProgress;

        // _strikeCdImage.enabled = strikeProgress >= 0;
        _strikeCdImage.fillAmount = strikeProgress;

        UpdateDashChargeSprite();
    }

    private async UniTask ShowPlayerHUD(bool showIcon = false)
    {
        await UniTask.Yield();

        PrepareHUDVisuals();

        if (showIcon)
        {
            SetupCharacterIcon();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_startFadeDelay));

        if (showIcon)
        {
            await AnimateIconOut();
        }

        await AnimateHUDIn();
    }

    private async UniTask AnimateIconOut()
    {
        await Tween.Scale(
            _characterIcon.transform,
            0f,
            _tweenDuration,
            _iconInEase
        );

        _characterIcon.enabled = false;
    }

    private async UniTask AnimateHUDIn()
    {
        _playerHUD.transform.localScale = Vector3.zero;

        await Tween.Scale(
            _playerHUD.transform,
            _scaleOne,
            _tweenDuration,
            _healthBarEase
        );
    }

    private void PrepareHUDVisuals()
    {
        _playerHUD.transform.localScale = Vector3.zero;
    }

    private void SetupCharacterIcon()
    {
        if (_characterIcon == null || _character == null) return;

        _characterIcon.transform.localScale = _scaleOne;
        _characterIcon.enabled = true;
        
        _characterIcon.sprite = _characterIcons[_character.Owner.PlayerIndex];
    }

    private void OnRoundEnded(RoundInfo roundInfo)
    {
        HidePlayerHUD().Forget();
    }
    
    private void OnRaceStart()
    {
        ShowPlayerHUD().Forget();
    }

    private async UniTask HidePlayerHUD()
    {
        await Tween.Scale(
            _playerHUD.transform,
            Vector3.zero,
            _tweenDuration,
            _healthBarEase
        );
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        float fillAmount = _character.Health.HealthRatio;

        if (_healthTween.isAlive && _healthTween.progress < 1f)
            _healthTween.Stop();

        _healthTween = Tween.Custom(_healthFillImg.fillAmount, fillAmount, _healthTweenDuration, newFillAmount =>
        {
            _healthFillImg.fillAmount = newFillAmount;
            _ticksMaterialInstance.SetFloat(FillID, newFillAmount);
        }, Ease.OutQuad);

        // Damage feedback
        float delta = newHealth - oldHealth;
        if (delta >= 0) return;

        RectTransform damageRect = _damageFillImg.rectTransform;

        float maxHealth = _character.Health.MaxHealth;
        float oldHealthNorm = Mathf.Clamp01(oldHealth / maxHealth);
        float newHealthNorm = Mathf.Clamp01(newHealth / maxHealth);

        float leftPx = newHealthNorm * _damageBarWidth;

        Vector2 offsetMin = damageRect.offsetMin;
        offsetMin.x = leftPx;
        damageRect.offsetMin = offsetMin;

        Vector2 offsetMax = damageRect.offsetMax;
        if (_damageTween.isAlive && _damageTween.progress < 1f)
        {
            _damageTween.Stop();
            offsetMax.x -= _previousDamageNormalized * (1f - _damageFillImg.fillAmount);
        }
        else
        {
            float rightPx = (1f - oldHealthNorm) * _damageBarWidth;
            offsetMax.x = -rightPx;
        }

        _previousDamageNormalized = _damageBarWidth - offsetMin.x + offsetMax.x;

        damageRect.offsetMax = offsetMax;
        _damageFillImg.fillAmount = 1f;

        _damageTween = Tween.UIFillAmount(_damageFillImg, 1f, 0f, _damageFbDepleteDuration, Ease.InOutQuad,
            startDelay: _damageFbDelay);
    }

    private void OnMaxHealthChanged(float _)
    {
        float baseHealth = _character.Stats.MaxHealth.BaseValue;
        float maxHealth = _character.Stats.MaxHealth.Value;

        float delta = maxHealth - baseHealth;
        float scalingY = 0.5f + delta / baseHealth * 0.5f;

        _ticksMaterialInstance.SetFloat(ScalingYID, scalingY);

        // Redraw health
        OnHealthChanged(0f, 0f);
    }

    private void OnDashChargeUpdated()
    {
        int dashCharges = Mathf.RoundToInt(_character.Stats.DashCharges.Value);

        _dashChargeBg.SetActive(dashCharges > 1);
        UpdateDashChargeSprite();
    }

    private void UpdateDashChargeSprite()
    {
        // Retrieve from character the amount of charges.
        int dashCharges = _character.AvailableDashCharges;

        // If we the amount hasn't change, no reason to change the sprite.
        if (_currentDashCharges == dashCharges)
            return;

        _currentDashCharges = dashCharges;

        Rect rect = _dashChargeTiledImg.uvRect;
        rect.x = 1f / k_maxDashCharges * dashCharges;
        _dashChargeTiledImg.uvRect = rect;
    }
}