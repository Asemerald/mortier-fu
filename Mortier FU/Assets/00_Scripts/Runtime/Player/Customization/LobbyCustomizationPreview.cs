using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu.Shared;
using UnityEngine;

namespace MortierFu
{
    public sealed class LobbyCustomizationPreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerCustomizationVisual _customizationVisual;

        [Header("Animations")]
        [Tooltip("(Optionnel) Si vide, cela lance l'animation de base de l'animator.")]
        [SerializeField] private string _enterStateName = "";

        [Tooltip("Nom de l'animation de sortie.")]
        [SerializeField] private string _exitStateName = "A_Player_Lobby_ReadyToSitting";

        [SerializeField] private float _enterDuration = 0.4f;
        [SerializeField] private float _exitDuration = 0.5f;

        private void Awake()
        {
            if (!_root)
                _root = gameObject;

            if (_root)
                _root.SetActive(false);
        }

        public async UniTask ShowAsync(
            PlayerCustomizationData customization,
            int colorIndex,
            CancellationToken cancellationToken
        )
        {
            if (_root)
                _root.SetActive(true);

            SetCustomColor(colorIndex);
            
            Apply(customization);

            PlayState(_enterStateName);

            if (_enterDuration > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_enterDuration),
                    cancellationToken: cancellationToken
                );
            }
        }

        public async UniTask HideAsync(CancellationToken cancellationToken)
        {
            if (!_root || !_root.activeSelf)
                return;

            PlayState(_exitStateName);

            if (_exitDuration > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_exitDuration),
                    cancellationToken: cancellationToken
                );
            }

            if (_root)
                _root.SetActive(false);
        }

        public void Apply(PlayerCustomizationData customization)
        {
            if (_customizationVisual)
                _customizationVisual.Apply(customization);
        }

        private void PlayState(string stateName)
        {
            if (!_animator)
                return;

            if (string.IsNullOrWhiteSpace(stateName))
                return;

            _animator.Play(stateName, 0, 0f);
        }

        public void SetCustomColor(int index)
        {
            if (_customizationVisual is null)
            {
                Logs.LogError("Customization Visual is null");
                return;
            }
            _customizationVisual.SetCustom(index);
        }
    }
}