namespace MortierFu
{
    public struct AugmentStack
    {
        public readonly SO_Augment Augment;
        public int Count;

        public AugmentStack(SO_Augment augment, int count)
        {
            Augment = augment;
            Count = count;
        }
    }
}