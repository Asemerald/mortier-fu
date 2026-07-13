using System;
using System.Collections.Generic;

namespace MortierFu
{
    public sealed class RaceModeContext
    {
        public IReadOnlyList<PlayerTeam> Teams;
        public PlayerTeam PreviousRoundWinnerTeam;

        public LevelSystem LevelSystem;
        public PlayerSpawnController PlayerSpawnController;
        public AugmentSelectionSystem AugmentSelectionSystem;
        public SO_GameFlowSettings FlowSettings;

        public Action<PlayerControlContext> SetAllPlayersControlContext;
        public Action<PlayerCharacter, float> ApplyBullySize;
        public Action ClearBullySize;
    }
}