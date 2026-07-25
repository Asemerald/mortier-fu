using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MortierFu
{
    public enum PlayerLobbyTutorialAction
    {
        Move,
        Aim,
        AimMoved,
        Shoot,
        Dash,
        Taunt
    }

    [CreateAssetMenu(fileName = "Tutorial", menuName = "Mortier Fu/UI/Tutorial")]
    public sealed class SO_Tutorial : ScriptableObject
    {
        [SerializeField] private PlayerLobbyTutorialAction _requiredAction;

        [SerializeField, FormerlySerializedAs("explanationText"), TextArea]
        private string _explanationText;

        [SerializeField, FormerlySerializedAs("spriteKeyboardGamePadUI")]
        private SpriteKeyboardGamePadUI _spriteKeyboardGamePadUI;

        public PlayerLobbyTutorialAction RequiredAction => _requiredAction;
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