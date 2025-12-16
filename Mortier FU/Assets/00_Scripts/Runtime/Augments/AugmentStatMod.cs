namespace MortierFu
{
    [System.Serializable]
    public struct AugmentStatMod
    {
        public float Value;
        public E_StatModType ModType;
    }
    
    public static class AugmentStatModExtensions {
        public static StatModifier ToMod(this AugmentStatMod mod, object source)
        {
            return new StatModifier(mod.Value, mod.ModType, source);
        }
    }
}
