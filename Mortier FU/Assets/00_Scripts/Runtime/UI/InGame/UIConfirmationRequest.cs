using System;
using Cysharp.Threading.Tasks;

namespace MortierFu
{
    public readonly struct UIConfirmationRequest
    {
        public PlayerManager Owner { get; }
        public string Description { get; }
        public string ConfirmLabel { get; }
        public string CancelLabel { get; }

        public Func<UniTask> OnConfirmAsync { get; }
        public Func<UniTask> OnCancelAfterCloseAsync { get; }

        public bool PauseGameWhileOpen { get; }
        public bool LockPlayersWhileOpen { get; }
        public bool RestoreContextOnConfirm { get; }
        public bool ResumeTimeScaleOnConfirm { get; }

        public UIConfirmationRequest(PlayerManager owner, string description, string confirmLabel, string cancelLabel, Func<UniTask> onConfirmAsync, Func<UniTask> onCancelAfterCloseAsync = null,
            bool pauseGameWhileOpen = true, bool lockPlayersWhileOpen = true, bool restoreContextOnConfirm = false, bool resumeTimeScaleOnConfirm = true)
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
        }
    }
}