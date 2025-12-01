namespace MortierFu
{
    public interface IInteractable
    {
        bool IsStrikeInteractable { get; }
        bool IsBombshellInteractable { get; }

        void Interact();
    }   
}