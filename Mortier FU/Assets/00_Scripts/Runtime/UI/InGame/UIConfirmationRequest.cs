using System;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public readonly struct UIConfirmationRequest
    {
        public readonly PlayerManager Owner;
        public readonly string Description;
        public readonly string ConfirmLabel;
        public readonly string CancelLabel;

        public readonly Func<UniTask> OnConfirmAsync;
        public readonly Func<UniTask> OnCancelAfterCloseAsync;

        public readonly bool PauseGameWhileOpen;
        public readonly bool LockPlayersWhileOpen;
        public readonly bool RestoreContextOnConfirm;
        public readonly bool ResumeTimeScaleOnConfirm;
        public readonly PlayerControlContext OwnerContext;

        public UIConfirmationRequest(PlayerManager owner, string description, string confirmLabel, string cancelLabel, Func<UniTask> onConfirmAsync, Func<UniTask> onCancelAfterCloseAsync = null,
            bool pauseGameWhileOpen = true, bool lockPlayersWhileOpen = true, bool restoreContextOnConfirm = false, bool resumeTimeScaleOnConfirm = true, PlayerControlContext ownerContext = PlayerControlContext.UIConfirmationOwner)
        {
            Owner = owner;
            Description = description;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
            OnConfirmAsync = onConfirmAsync;
            OnCancelAfterCloseAsync = onCancelAfterCloseAsync;
            PauseGameWhileOpen = pauseGameWhileOpen;
            LockPlayersWhileOpen = lockPlayersWhileOpen;
            RestoreContextOnConfirm = restoreContextOnConfirm;
            ResumeTimeScaleOnConfirm = resumeTimeScaleOnConfirm;
            OwnerContext = ownerContext;
        }
    }
}