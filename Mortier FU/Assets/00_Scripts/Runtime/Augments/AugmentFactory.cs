namespace MortierFu
{
    public static class AugmentFactory
    {
        public static IAugment Create(DA_Augment augment, Character owner)
        {
            if (augment == null || augment.AugmentType.Type == null)
            {
                throw new System.ArgumentNullException(nameof(augment), "Augment data or type cannot be null");
            }

            var augmentInstance = (IAugment)System.Activator.CreateInstance(augment.AugmentType, augment, owner);
            return augmentInstance;
        }
    }
}