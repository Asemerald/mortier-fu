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

    private readonly Vector3 _scaleOne = Vector3.one;

    private void OnEnable()
    {
        if (_character == null)
        {
            enabled = false;
            Logs.LogWarning("Missing character reference on PlayerGameplayUI. Falling asleep.", this);
            return;
        }

        _character.Health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (_character == null) return;
        _character.Health.OnHealthChanged -= OnHealthChanged;
    }

    private void Start()
    {
        _playerHUD.sprite = _playerHUDSprites[_character.Owner.PlayerIndex];
        GetColorAndIndex().Forget();
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
        _healthFillImg.fillAmount = _character.Health.HealthRatio;
    }
}