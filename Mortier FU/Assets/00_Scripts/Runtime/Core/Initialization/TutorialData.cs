using System;

[Serializable]
public class TutorialData
{
    public bool TutorialFirstRaceDone = false;
    public bool TutorialSecondRaceDone = false;
    public bool TutorialPinataRaceDone = false;
    public bool TutorialObstacleRaceDone = false;
    
    public static TutorialData CreateTutorialData() => new TutorialData();
}
