using System;
using UnityEngine;

[Serializable]
public class TutorialCardData
{
    public string Name;
    [TextArea] public string DescriptionKey;
    public Sprite MainImage;
}