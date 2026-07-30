using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "MortierFu/Tutorial/Tutorial Config", fileName = "SO_TutorialConfig")]
public class SO_TutorialConfig : ScriptableObject
{
    public List<SO_TutorialStepData> Steps;

    public SO_TutorialStepData GetStepData(AugmentRaceTutorialType type) => Steps.FirstOrDefault(step => step.TutorialType == type);
}