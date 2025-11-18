namespace MortierFu
{
    public static class AugmentFactory
    {
        public static IAugment Create(SO_Augment augment, PlayerCharacter owner, SO_AugmentDatabase db)
        {
            if (augment == null || augment.AugmentType.Type == null)
            {
                throw new System.ArgumentNullException(nameof(augment), "Augment data or type cannot be null");
            }

            var augmentInstance = (IAugment)System.Activator.CreateInstance(augment.AugmentType.Type, augment, owner, db);
            return augmentInstance;
        }
    }
}