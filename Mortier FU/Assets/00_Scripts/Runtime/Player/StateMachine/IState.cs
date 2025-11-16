using System;

namespace MortierFu
{
    public interface IState : IDisposable
    {
        void OnEnter();
        void Update();
        void FixedUpdate();
        void OnExit();
    }   
}
