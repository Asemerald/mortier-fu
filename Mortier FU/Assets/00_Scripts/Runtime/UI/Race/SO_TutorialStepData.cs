using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MortierFu/Tutorial/Tutorial Step Data", fileName = "SO_TutorialStep_")]
public class SO_TutorialStepData : ScriptableObject
{
    public AugmentRaceTutorialType TutorialType;
    public List<TutorialCardData> Cards;
}