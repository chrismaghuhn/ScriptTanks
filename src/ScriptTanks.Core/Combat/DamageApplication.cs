using System;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Deterministic helper for applying raw incoming damage to a tank state.
/// This helper performs integer-only armor reduction and only mutates HP
/// immutably through <see cref="TankState.WithCurrentHitPoints(int)"/>.
/// It does not create combat logs, death events, armor-piercing behavior,
/// damage-type effects, weapon effects, or match-level side effects.
/// Truncation to zero effective damage intentionally leaves HP unchanged;
/// no minimum-damage floor is applied. HP is floored at zero, so overkill
/// damage maps to a destroyed state without throwing.
/// <para>
/// Raw damage values larger than <c>int.MaxValue / 100</c> are rejected to
/// keep the multiplication <c>rawDamage * (100 - armorReductionPercent)</c>
/// safe in <see cref="int"/> math without introducing <see cref="long"/>,
/// <c>Int128</c> or <c>checked</c> arithmetic.
/// </para>
/// </summary>
public static class DamageApplication
{
    public static TankState Apply(TankState target, int rawDamage)
    {
        if (rawDamage < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawDamage),
                rawDamage,
                "Raw damage must not be negative.");
        }

        if (rawDamage > int.MaxValue / 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawDamage),
                rawDamage,
                "Raw damage is too large for deterministic int-based damage calculation.");
        }

        int armorReductionPercent = target.Definition.Stats.ArmorReductionPercent;
        int effectiveDamage = rawDamage * (100 - armorReductionPercent) / 100;
        int newHitPoints = target.CurrentHitPoints - effectiveDamage;

        if (newHitPoints < 0)
        {
            newHitPoints = 0;
        }

        return target.WithCurrentHitPoints(newHitPoints);
    }
}
