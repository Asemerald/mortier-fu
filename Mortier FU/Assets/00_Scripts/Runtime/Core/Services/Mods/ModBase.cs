// Classe de base pour tous les mods

namespace MortierFu
{
    public abstract class ModBase
    {
        public abstract void Initialize();
        public virtual void DeInitialize() { }

        public virtual void ModUpdate() { }
    }
}