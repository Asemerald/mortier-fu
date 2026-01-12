namespace MortierFu
{
    public interface IInteractable
    {
        bool IsDashInteractable { get; }
        bool IsBombshellInteractable { get; }

        void Interact();
    }   
}