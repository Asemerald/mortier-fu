using System;
using System.Collections.Generic;
using UnityEngine;


public enum E_AugmentVariable
{
    Health, LifeSteal, MoveSpeed, // DURABILITY
    BulletSpeed, Damage, ImpactRadius, FireRate, ShotRange, // BOMBSHELL 
    Bounce, BouncySnowball, SelfBounce, // BOUNCE
    StrikeForce, DashCooldown, DashForce, ExtraDash, // DASH
    KnockbackStunDuration, // STUN
    OnEnemyHit, AfterDashCondition, OnEnemyStun, AfterTenSecondsCondition, // CONDITION
    Empty
}

public enum E_AugmentValue
{
    Empty, MinusThree, MinusTwo, MinusOne, PlusOne, PlusTwo, PlusThree, OneNumber
}

public static class AugmentVariableDescription
{
    private static readonly Dictionary<E_AugmentVariable, string> Description = new()
    {
        { E_AugmentVariable.Health, "Health" },
        { E_AugmentVariable.LifeSteal, "Life Steal" },
        { E_AugmentVariable.MoveSpeed, "Move Speed" },
        { E_AugmentVariable.BulletSpeed, "Bullet Speed" },
        { E_AugmentVariable.Damage, "Damage" },
        { E_AugmentVariable.ImpactRadius, "Impact Radius" },
        { E_AugmentVariable.FireRate, "Fire Rate" },
        { E_AugmentVariable.ShotRange, "Shot Range" },
        { E_AugmentVariable.Bounce, "Bounce" },
        { E_AugmentVariable.BouncySnowball, "For Each Bounce, Impact Radius" },
        { E_AugmentVariable.SelfBounce, "For Each Bounce" },
        { E_AugmentVariable.StrikeForce, "Push Force"},
        { E_AugmentVariable.DashCooldown, "Dash Cooldown" },
        { E_AugmentVariable.DashForce, "Dash Distance" },
        { E_AugmentVariable.ExtraDash, "Extra Dash" },
        { E_AugmentVariable.KnockbackStunDuration, "Stun Duration" },
        { E_AugmentVariable.OnEnemyHit, "When Hit Enemy" },
        { E_AugmentVariable.AfterDashCondition, "1.5s After A Dash"},
        { E_AugmentVariable.OnEnemyStun , "When Stun Enemy"},
        { E_AugmentVariable.AfterTenSecondsCondition, "After 10 Seconds : "},
        { E_AugmentVariable.Empty, ""},
    };
    
    public static string Get(E_AugmentVariable variable) => Description[variable];
}

[Serializable]
public struct AugmentDescription
{
    public E_AugmentVariable variable;
    public E_AugmentValue value;
    public int TextSize;
    public int SymbolSize;
}