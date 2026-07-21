using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "Tutorial", menuName = "Mortier Fu/UI/Tutorial")]
public class SO_Tutorial : ScriptableObject
{
    [Space(10)]
    public List<InputActionReference> inputAction;
    [Space(10)]
    public String explanationText;
    [Space(10)]
    public SpriteKeyboardGamePadUI spriteKeyboardGamePadUI;

    [Space(10)]
    public bool connectedToActionBefore;

    //helper methods
    
    public Sprite GetSpriteByInput(bool isKeyboard)
    {
        return isKeyboard ?
            spriteKeyboardGamePadUI.spriteKeyboard :
            spriteKeyboardGamePadUI.spriteGamePad;
    }
    
    public Vector2 GetSizeByInput(bool isKeyboard)
    {
        return isKeyboard ?
            spriteKeyboardGamePadUI.spriteKeyboardSize :
            spriteKeyboardGamePadUI.spriteGamePadSize;
    }
}

[Serializable]
public struct SpriteKeyboardGamePadUI
{
    public Sprite spriteGamePad;
    public Vector2 spriteGamePadSize;
    [Space(10)]
    public Sprite spriteKeyboard;
    public Vector2 spriteKeyboardSize;
}