using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MortierFu;
using MortierFu.Shared;
using UnityEngine.SceneManagement;

public enum AugmentRaceTutorialType
{
    FirstRace,
    SecondRace,
    Pinata,
    Obstacle
}

public class AugmentRaceTutorialController
{
    public event Action<AugmentRaceTutorialType> OnTutorialStepStarted;
    public event Action<AugmentRaceTutorialType> OnTutorialStepCompleted;

    public AugmentRaceTutorialController(GameModeBase gm)
    {
        gm.OnTutorialRaceControllerInit?.Invoke(this);
    }
    
    
    public async UniTask WaitForTutorialCheck(CancellationToken cancellationToken)
{
    SaveService saveService = ServiceManager.Instance.Get<SaveService>();
    LevelSystem levelSystem = SystemManager.Instance.Get<LevelSystem>();
    ConfirmationService confirmationService = ServiceManager.Instance.Get<ConfirmationService>();

    if (saveService is null)
    {
        Logs.LogError("[AugmentRaceController] Save service was not found.");
        return;
    }

    if (saveService.Tutorial is null)
    {
        Logs.LogError("[AugmentRaceController] Tutorial Data is null.");
        return;
    }

    if (levelSystem is null)
    {
        Logs.LogError("[AugmentRaceController] Level System was not found.");
        return;
    }

    if (confirmationService is null)
    {
        Logs.LogError("[AugmentRaceController] Confirmations service was not found.");
        return;
    }

    cancellationToken.ThrowIfCancellationRequested();

    SO_RaceModeDefinition contextRace = levelSystem.CurrentRaceReporter.RaceModeDefinition;
    
    bool displayFirstTutorial = contextRace is SO_ClassicRaceModeDefinition &&
                                !saveService.Tutorial.TutorialFirstRaceDone;

    if (displayFirstTutorial)
    {
        await RunTutorialStep(saveService, confirmationService, AugmentRaceTutorialType.FirstRace,
            () => saveService.Tutorial.TutorialFirstRaceDone = true, cancellationToken);
        return;
    }

    bool displaySecondTutorial = contextRace is SO_ClassicRaceModeDefinition &&
                                 saveService.Tutorial.TutorialFirstRaceDone &&
                                 !saveService.Tutorial.TutorialSecondRaceDone;

    if (displaySecondTutorial)
    {
        await RunTutorialStep(saveService, confirmationService, AugmentRaceTutorialType.SecondRace,
            () => saveService.Tutorial.TutorialSecondRaceDone = true, cancellationToken);
        return;
    }

    bool displayPinataTutorial = contextRace is SO_PinhataRaceModeDefinition &&
                                 !saveService.Tutorial.TutorialPinataRaceDone;

    if (displayPinataTutorial)
    {
        await RunTutorialStep(saveService, confirmationService, AugmentRaceTutorialType.Pinata,
            () => saveService.Tutorial.TutorialPinataRaceDone = true, cancellationToken);
        return;
    }

    bool displayObstacleTutorial = contextRace is SO_StaticBullyMazeRaceModeDefinition &&
                                   !saveService.Tutorial.TutorialObstacleRaceDone;

    if (displayObstacleTutorial)
    {
        await RunTutorialStep(saveService, confirmationService, AugmentRaceTutorialType.Obstacle,
            () => saveService.Tutorial.TutorialObstacleRaceDone = true, cancellationToken);
        return;
    }
}

    private async UniTask RunTutorialStep(SaveService saveService, ConfirmationService confirmationService,
        AugmentRaceTutorialType tutorialType, Action markTutorialDone, CancellationToken cancellationToken)
    {
        List<PlayerManager> players = confirmationService.GetAvailablePlayers();

        OnTutorialStepStarted?.Invoke(tutorialType);

        confirmationService.ShowConfirmation(players.Count);

        UniTask<bool> confirmationTask = confirmationService.WaitUntilAllConfirmed().AsUniTask();
        UniTask cancellationTask = UniTask.WaitUntilCanceled(cancellationToken);

        (bool hasResultLeft, bool allConfirmed) = await UniTask.WhenAny(confirmationTask, cancellationTask);

        if (!hasResultLeft)
        {
            confirmationService.ResetRuntimeState();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!allConfirmed)
        {
            Logs.LogWarning("[AugmentRaceController] Tutorial confirmation was interrupted or reset.");
            return;
        }

        markTutorialDone();

        if (saveService.Tutorial.IsTutorialFinished()) await saveService.SaveTutorial();
        
        confirmationService.ResetRuntimeState();

        OnTutorialStepCompleted?.Invoke(tutorialType);
    }
}