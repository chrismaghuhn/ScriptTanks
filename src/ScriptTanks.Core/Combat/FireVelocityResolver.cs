using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Pure, read-only resolver for deterministic fire velocity derivation from turret
/// aim direction and weapon projectile speed.
/// </summary>
/// <remarks>
/// Read-only inspection of <see cref="MatchSensorRuntimeState"/> and tank weapon loadouts.
/// Resolves <see cref="FixedVec2"/> fire velocity as forward direction from
/// <see cref="TankState.TurretRotation"/> scaled by
/// <see cref="WeaponDefinition.ProjectileSpeedPerTick"/>. Does not use
/// <see cref="TankState.BodyRotation"/> as fallback. The
/// <see cref="FireVelocityStatus.MissingWeaponSpeed"/> branch is defensive for
/// non-positive speed at resolve time; catalog <see cref="WeaponDefinition"/>
/// instances are constructed with positive speed. Does not construct
/// <see cref="MatchFireRequest"/>, allocate projectile ids, execute fire, mutate
/// match or sensor runtime state, log, replay, or integrate Godot.
/// </remarks>
public static class FireVelocityResolver
{
    public static FireVelocityResult Resolve(
        MatchSensorRuntimeState runtime,
        int tankIndex,
        WeaponSlot weaponSlot)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        MatchState state = runtime.State;
        int tankCount = state.Tanks.Count;

        if (tankIndex < 0 || tankIndex >= tankCount)
        {
            return FireVelocityResult.TankIndexOutOfRange();
        }

        TankState tank = state.Tanks[tankIndex];

        if (tank.IsDestroyed)
        {
            return FireVelocityResult.TankDestroyed();
        }

        TankWeaponLoadout loadout = state.Loadouts[tankIndex];

        if (weaponSlot.Value >= loadout.Weapons.Count)
        {
            return FireVelocityResult.WeaponSlotMissing();
        }

        WeaponState weapon = loadout.GetWeapon(weaponSlot);

        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(tank.TurretRotation);

        if (!forwardResult.IsResolved)
        {
            return FireVelocityResult.MissingAimDirection();
        }

        Fixed speed = weapon.Definition.ProjectileSpeedPerTick;
        if (speed <= Fixed.Zero)
        {
            return FireVelocityResult.MissingWeaponSpeed();
        }

        FixedVec2 forward = forwardResult.Forward!.Value;
        FixedVec2 velocity = forward * speed;
        return FireVelocityResult.Resolved(velocity);
    }
}
