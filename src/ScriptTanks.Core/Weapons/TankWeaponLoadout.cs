using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptTanks.Core.Weapons;

/// <summary>
/// Immutable ordered collection of runtime <see cref="WeaponState"/>
/// instances that make up a single tank's loadout. The class is a pure
/// data carrier: it does not know which tank owns it, performs no fire
/// validation, projectile spawning, ammo handling, hardpoint geometry,
/// scripting integration, or match-level side effects. Slot lookup uses
/// <see cref="WeaponSlot.Value"/> as a list index against the defensively
/// copied weapon array.
/// </summary>
public sealed class TankWeaponLoadout
{
    public IReadOnlyList<WeaponState> Weapons { get; }

    public TankWeaponLoadout(IEnumerable<WeaponState> weapons)
    {
        ArgumentNullException.ThrowIfNull(weapons);

        WeaponState[] weaponArray = weapons.ToArray();

        if (weaponArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one weapon is required.",
                nameof(weapons));
        }

        Weapons = weaponArray;
    }

    public WeaponState GetWeapon(WeaponSlot slot)
    {
        if (slot.Value >= Weapons.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Weapon slot is outside the loadout.");
        }

        return Weapons[slot.Value];
    }

    public TankWeaponLoadout WithWeapon(WeaponSlot slot, WeaponState weapon)
    {
        if (slot.Value >= Weapons.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Weapon slot is outside the loadout.");
        }

        WeaponState[] copy = Weapons.ToArray();
        copy[slot.Value] = weapon;
        return new TankWeaponLoadout(copy);
    }
}
