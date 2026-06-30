namespace MortierFu
{
    public interface IPlayerInteractionHandler
    {
        bool CanHandleInteraction(PlayerManager player);
        bool HandleInteract(PlayerManager player);
    }
}