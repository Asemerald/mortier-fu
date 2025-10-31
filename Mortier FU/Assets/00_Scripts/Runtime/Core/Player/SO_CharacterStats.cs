using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_PlayerData", menuName = "Mortier Fu/Player Data", order = 0)]
    public class SO_CharacterStats : ScriptableObject
    {
        [Header("Character Statistics")]
        [field: SerializeField, Tooltip("Maximum health of the player.")]
        public CharacterStat MaxHealth { get; private set; } = new(4.0f);

        [field: SerializeField, Tooltip("Maximum move speed in units per second.")]
        public CharacterStat MoveSpeed { get; private set; } = new(5.0f);
        
        [field: SerializeField, Tooltip("The greater the acceleration, the faster the character reaches the move speed.")]
        public CharacterStat MoveAcceleration { get; private set; } = new(6.0f);
        
        [field: SerializeField, Tooltip("The greater the drag, the faster the character stops moving.")]
        public CharacterStat MoveDrag { get; private set; } = new(4.0f);
        
        [field: SerializeField, Tooltip("Scale applied to the player avatar.")]
        public CharacterStat AvatarSize { get; private set; } = new(1.0f);
        
        [Header("Mortar Statistics")]
        [field: SerializeField, Tooltip("Damage dealt by a single mortar shot.")]
        public CharacterStat DamageAmount { get; private set; } = new(1.0f);
        
        [field: SerializeField, Tooltip("Radius of the area-of-effect damage.")]
        public CharacterStat DamageRange { get; private set; } = new(2.0f);
        
        [field: SerializeField, Tooltip("Cooldown between each shot.")]
        public CharacterStat FireRate { get; private set; } = new(2.0f);
        
        [field: SerializeField, Tooltip("Maximum effective range of mortar shots.")]
        public CharacterStat ShotRange { get; private set; } = new(20.0f);
        
        [field: SerializeField, Tooltip("Speed of the projectile after being fired.")]
        public CharacterStat ProjectileTimeTravel { get; private set; } = new(3.0f);
        
        [field: SerializeField, Tooltip("Speed at which the aim widget moves.")]
        public CharacterStat AimWidgetSpeed { get; private set; } = new(7.0f);
        
        [field: SerializeField, Tooltip("Damage of the Strike attack.")]
        public CharacterStat StrikeDamage { get; private set; } = new(.0f);
        
        [field: SerializeField, Tooltip("Duration of the Strike attack.")]
        public CharacterStat StrikeDuration { get; private set; } = new( 0.2f);
        
        [field: SerializeField, Tooltip("Cooldown of the Strike attack.")]
        public CharacterStat StrikeCooldown { get; private set; } = new( 2.0f);
        
        [field: SerializeField, Tooltip("Cooldown of the Strike attack.")]
        public CharacterStat StrikeRadius { get; private set; } = new( 2.0f);
        
        [field: SerializeField, Tooltip("Duration of the Stun.")]
        public CharacterStat StunDuration { get; private set; } = new( 0.5f);
        
        [field: SerializeField, Tooltip("Amount of bullets each shot will launch.")]
        public CharacterStat BulletNumber { get; private set; } = new( 1.0f);
    }
}
