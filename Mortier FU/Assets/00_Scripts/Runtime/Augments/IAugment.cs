using System;

namespace MortierFu
{
    public interface IAugment : IDisposable
    {
        void Initialize();

        void Reset();
    }
}