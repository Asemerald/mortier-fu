using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    public enum PlayerLobbyTutorialAction
    {
        Move,
        Aim,
        AimReleased,
        AimMoved,
        Shoot,
        Dash,
        Taunt
    }

    [CreateAssetMenu(fileName = "Tutorial", menuName = "Mortier Fu/UI/Tutorial")]
    public sealed class SO_Tutorial : ScriptableObject
    {
        [Header("Step")]
        [SerializeField] private PlayerLobbyTutorialAction _requiredAction;

        [Header("Rules")]
        [SerializeField] private bool _skipIfAlreadyPerformed = true;
        [SerializeField] private bool _requiresAimHeld;
        [SerializeField] private bool _returnToAimStepWhenAimReleased;

        [Header("Text")]
        [SerializeField, TextArea]
        private string _explanationText;

        [Header("Sprites")]
        [SerializeField]
        private SpriteKeyboardGamePadUI _spriteKeyboardGamePadUI;

        public PlayerLobbyTutorialAction RequiredAction => _requiredAction;
        public bool SkipIfAlreadyPerformed => _skipIfAlreadyPerformed;
        public bool RequiresAimHeld => _requiresAimHeld;
        public bool ReturnToAimStepWhenAimReleased => _returnToAimStepWhenAimReleased;
        public string ExplanationText => _explanationText;

        public Sprite GetSpriteByInput(bool isKeyboard) => isKeyboard ? _spriteKeyboardGamePadUI.SpriteKeyboard : _spriteKeyboardGamePadUI.SpriteGamePad;

        public Vector2 GetSizeByInput(bool isKeyboard) => isKeyboard ? _spriteKeyboardGamePadUI.SpriteKeyboardSize : _spriteKeyboardGamePadUI.SpriteGamePadSize;
    }

    [Serializable]
    public struct SpriteKeyboardGamePadUI
    {
        [FormerlySerializedAs("spriteGamePad")]
        [SerializeField] private Sprite _spriteGamePad;

        [FormerlySerializedAs("spriteGamePadSize")]
        [SerializeField] private Vector2 _spriteGamePadSize;

        [FormerlySerializedAs("spriteKeyboard")]
        [SerializeField] private Sprite _spriteKeyboard;

        [FormerlySerializedAs("spriteKeyboardSize")]
        [SerializeField] private Vector2 _spriteKeyboardSize;

        public Sprite SpriteGamePad => _spriteGamePad;
        public Vector2 SpriteGamePadSize => _spriteGamePadSize;
        public Sprite SpriteKeyboard => _spriteKeyboard;
        public Vector2 SpriteKeyboardSize => _spriteKeyboardSize;
    }
}