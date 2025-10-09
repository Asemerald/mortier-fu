using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_PlayerData", menuName = "Mortier Fu/Player Data", order = 0)]
    public class DA_CharacterData : ScriptableObject
    {
        [Header("Character Statistics")]
        [field: SerializeField, Tooltip("Maximum health of the player.")]
        public CharacterStat MaxHealth { get; private set; } = new(100.0f);

        [field: SerializeField, Tooltip("Base movement speed in units per second.")]
        public CharacterStat MoveSpeed { get; private set; } = new(5.0f);
        
        [field: SerializeField, Tooltip("Scale applied to the player avatar.")]
        public CharacterStat AvatarSize { get; private set; } = new(1.0f);
        
        [Header("Mortar Statistics")]
        [field: SerializeField, Tooltip("Damage dealt by a single mortar shot.")]
        public CharacterStat Damage { get; private set; } = new(30.0f);
        
        [field: SerializeField, Tooltip("Cooldown between each shot.")]
        public CharacterStat AttackSpeed { get; private set; } = new(2.0f);
        
        [field: SerializeField, Tooltip("Maximum effective range of mortar shots.")]
        public CharacterStat ShotRange { get; private set; } = new(20.0f);
        
        [field: SerializeField, Tooltip("Speed of the projectile after being fired.")]
        public CharacterStat ProjectileSpeed { get; private set; } = new(8.0f);
        
        [field: SerializeField, Tooltip("Radius of the area-of-effect damage.")]
        public CharacterStat AOERange { get; private set; } = new(2.0f);
        
        [field: SerializeField, Tooltip("Speed at which the aim widget moves.")]
        public CharacterStat AimWidgetSpeed { get; private set; } = new(7.0f);
    }
}
