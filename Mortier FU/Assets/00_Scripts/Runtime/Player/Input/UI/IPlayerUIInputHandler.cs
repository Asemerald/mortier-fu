using UnityEngine;

namespace MortierFu
{
    public interface IPlayerUIInputHandler
    {
        bool CanHandleUIInput(PlayerManager player);

        bool HandleNavigate(PlayerManager player, Vector2 direction);
        bool HandleSubmit(PlayerManager player);
        bool HandleCancel(PlayerManager player);
    }
}