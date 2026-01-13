using UnityEngine;

namespace MortierFu
{
    [CreateAssetMenu(fileName = "DA_PlayerData", menuName = "Mortier Fu/Player Data", order = 0)]
    public class SO_CharacterStats : ScriptableObject
    {
        [Header("Character Statistics")]
        [field: SerializeField, Tooltip("Maximum health of the player.")]
        public CharacterStat MaxHealth { get; private set; } = new(4.0f);

        [field: SerializeField, Tooltip("Base movement speed in units per second.")]
        public CharacterStat MoveSpeed { get; private set; } = new(5.0f);
        
        [field: SerializeField, Tooltip("Base acceleration.")]
        public CharacterStat Accel { get; private set; } = new(8.0f);
        
        [field: SerializeField, Tooltip("Base deceleration.")]
        public CharacterStat Decel { get; private set; } = new(6.0f);
        
        [field: SerializeField, Tooltip("Scale applied to the player avatar.")]
        public CharacterStat AvatarSize { get; private set; } = new(1.0f);
        
        [Header("Mortar Statistics")]
        [field: SerializeField, Tooltip("Damage dealt by a single mortar shot.")]
        public CharacterStat BombshellDamage { get; private set; } = new(1.0f);

        [field: SerializeField, Tooltip("Size of the projectile")]
        public CharacterStat BombshellSize { get; private set; } = new(0.6f);
        
        [field: SerializeField, Tooltip("Radius of the area-of-effect damage.")]
        public CharacterStat BombshellImpactRadius { get; private set; } = new(2.0f);
        
        [field: SerializeField, Tooltip("Amount of bullets each shot will launch.")]
        public CharacterStat BombshellBounces { get; private set; } = new( 0.0f);
        
        [field: SerializeField, Tooltip("Cooldown between each shot.")]
        public CharacterStat FireRate { get; private set; } = new(5.0f);
        
        [field: SerializeField, Tooltip("Maximum effective range of mortar shots.")]
        public CharacterStat ShotRange { get; private set; } = new(20.0f);
        
        [field: SerializeField, Tooltip("Speed of the bombshell after being fired.")]
        public CharacterStat BombshellSpeed { get; private set; } = new(10.0f);
        
        [field: SerializeField, Tooltip("Speed at which the aim widget moves.")]
        public CharacterStat AimWidgetSpeed { get; private set; } = new(7.0f);
        
        [field: SerializeField, Tooltip("Damage of the Strike attack.")]
        public CharacterStat StrikeDamage { get; private set; } = new(.0f);
        
        [field: SerializeField, Tooltip("Amount of charges of dash.")]
        public CharacterStat DashCharges { get; private set; } = new( 1.0f);
        
        [field: SerializeField, Tooltip("Cooldown of the Strike attack.")]
        public CharacterStat DashCooldown { get; private set; } = new( 2.0f);
        
        [field: SerializeField, Tooltip("Duration of the Strike attack.")]
        public CharacterStat DashDuration { get; private set; } = new( 0.2f);
        
        [field: SerializeField, Tooltip("Radius of the Strike while dashing.")]
        public CharacterStat StrikeRadius { get; private set; } = new( 2.0f);
        
        [field: SerializeField, Tooltip("The force used to push other characters.")]
        public CharacterStat StrikePushForce { get; private set; } = new( 2f);

        [field: SerializeField, Tooltip("Offset the strength to make it more scalable.")]
        public float StrikePushForceOffset = 8.5f;

        [field: SerializeField, Tooltip("Duration of the Knockback effect.")]
        public CharacterStat StrikeKnockbackDuration { get; private set; } = new(0.5f);
        
        [field: SerializeField, Tooltip("Stun duration caused when colliding into an obstacle during knockback.")]
        public float KnockbackStunDuration { get; private set; } = 0.5f;
        
        [field: Header("Fomrula Components"), SerializeField, Tooltip("Influence of the max health towards the avatar size.")]
        public float MaxHealthToAvatarSizeFactor { get; private set; } = 0.6f;
        
        [field: SerializeField, Tooltip("Influence of strike push force towards the strike radius.")]
        public float StrikePushForceToStrikeRadiusFactor { get; private set; } = 0.6f;
        
        [field: SerializeField, Tooltip("Influence of strike push force towards the strike radius.")]
        public float AvatarSizeToStrikeRadiusFactor { get; private set; } = 1f;
        
        [field: SerializeField, Tooltip("Influence of the bombshell impact radius towards the shot range.")]
        public float BombshellImpactRadiusToShotRangeFactor { get; private set; } = 0.8f;
        
        [field: SerializeField, Tooltip("Influence of the bombshell impact radius towards the bombshell size.")]
        public float BombshellImpactRadiusToBombshellSizeFactor { get; private set; } = 1.4f;
        
        [field: SerializeField, Tooltip("Factor that determines how much the forces applied to this character will be mitigated based on the avatar size.")]
        public float AvatarSizeToForceMitigationFactor { get; private set; } = 0.2f;
        
        // Complex stats calculations
        public float GetBombshellSpeed() => BombshellSpeed.Value * 0.1f;
        public float GetAvatarSize()     => AvatarSize.Value + (MaxHealth.Value - MaxHealth.BaseValue) * MaxHealthToAvatarSizeFactor;
        public float GetFireRate()       => 10f / FireRate.Value;
        public float GetShotRange()      => ShotRange.Value + (BombshellImpactRadius.Value - BombshellImpactRadius.BaseValue) * BombshellImpactRadiusToShotRangeFactor;
        public float GetBombshellSize()  => BombshellSize.Value + (BombshellImpactRadius.Value - BombshellImpactRadius.BaseValue) * BombshellImpactRadiusToBombshellSizeFactor;
        public float GetDashCooldown()   => 10f / DashCooldown.Value;
        public float GetDashPushForce()  => StrikePushForceOffset + StrikePushForce.Value;
        public float GetStrikeRadius()   => StrikeRadius.Value + (AvatarSize.Value - AvatarSize.BaseValue + StrikePushForce.Value - StrikePushForce.BaseValue) * StrikePushForceToStrikeRadiusFactor;
        public float GetKnockbackStunDuration()
        {
            float factor = KnockbackStunDuration / (StrikePushForce.BaseValue + StrikePushForceOffset);
            return KnockbackStunDuration + StrikePushForce.Value * factor;
        }
    }
}
