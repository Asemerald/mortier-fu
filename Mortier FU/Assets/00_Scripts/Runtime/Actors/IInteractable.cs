using UnityEngine;
namespace MortierFu
{
    public interface IInteractable
    {
        bool IsDashInteractable { get; }
        bool IsBombshellInteractable { get; }

        void Interact(Vector3 contactPoint);
    }   
}