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
    [Header("References")] [SerializeField]
    private PlayerCharacter _character;

    [SerializeField] private Image _healthFillImg;
    [SerializeField] private Image _healthTicksImg;
    private Material _ticksMaterialInstance;
    [SerializeField] private Image _playerHUD;
    [SerializeField] private Image _strikeCdImage;
    [SerializeField] private Image _shootCdImage;
    [SerializeField] private Image _characterIcon;
    [SerializeField] private TMP_Text _playerIndexText;

    [SerializeField] private Sprite[] _characterIcons;
    [SerializeField] private Sprite[] _playerHUDSprites;

    [SerializeField] private float _tweenDuration = 0.5f;
    [SerializeField] private float _startFadeDelay = 2f;

    [SerializeField] private Ease _iconInEase = Ease.InBack;
    [SerializeField] private Ease _healthBarEase = Ease.OutBack;

    [Header("Dash")]
    [SerializeField] private GameObject _dashChargeBg;
    [SerializeField] private RawImage _dashChargeTiledImg;
    private int _currentDashCharges = 0;
    private const int k_maxDashCharges = 7;

    private readonly Vector3 _scaleOne = Vector3.one;

    private static readonly int FillID = Shader.PropertyToID("_Fill");
    private static readonly int ScalingYID = Shader.PropertyToID("_ScalingY");
    
    private void OnEnable()
    {
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
    }

    private void Start()
    {
        _playerHUD.sprite = _playerHUDSprites[_character.Owner.PlayerIndex];
        GetColorAndIndex().Forget();

        OnHealthChanged(1f);
        OnMaxHealthChanged(1f);
    }

    private void Update()
    {
        // Reverse progress bar
        float strikeProgress = 1 - _character.GetStrikeCooldownProgress;
        float shootProgress = 1 - _character.Mortar.ShootCooldownProgress;

        // _strikeCdImage.enabled = strikeProgress >= 0;
        _strikeCdImage.fillAmount = strikeProgress;

        // _shootCdImage.enabled = shootProgress >= 0f;
        _shootCdImage.fillAmount = shootProgress;

        UpdateDashChargeSprite();
    }

    private async UniTaskVoid GetColorAndIndex()
    {
        await UniTask.Yield();

        _playerHUD.transform.localScale = Vector3.zero;

        _characterIcon.transform.localScale = _scaleOne;

        _characterIcon.enabled = true;

        if (_characterIcon != null && _character != null)
        {
            _characterIcon.sprite = _characterIcons[_character.Owner.PlayerIndex];
        }

        if (_playerIndexText != null)
        {
            string playerIndexStr = "Player " + (_character.Owner.PlayerIndex + 1);
            _playerIndexText.text = playerIndexStr;
        }

        await UniTask.Delay(TimeSpan.FromSeconds(_startFadeDelay));

        await Tween.Scale(_characterIcon.transform, 0f, _tweenDuration, _iconInEase);

        await Tween.Scale(_playerHUD.transform, _scaleOne, _tweenDuration, _healthBarEase);

        _characterIcon.enabled = false;
    }

    private void OnHealthChanged(float amount)
    {
        float fillAmount = _character.Health.HealthRatio;
        
        _healthFillImg.fillAmount = fillAmount;
        _ticksMaterialInstance.SetFloat(FillID, fillAmount);
    }
    
    private void OnMaxHealthChanged(float _)
    {
        float baseHealth = _character.Stats.MaxHealth.BaseValue;
        float maxHealth = _character.Stats.MaxHealth.Value;

        float delta = maxHealth - baseHealth;
        float scalingY = 0.5f + delta / baseHealth * 0.5f;
        
        _ticksMaterialInstance.SetFloat(ScalingYID, scalingY);
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