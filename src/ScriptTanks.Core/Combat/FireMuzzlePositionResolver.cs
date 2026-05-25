using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Combat;

/// <summary>
/// Pure, read-only resolver for deterministic fire muzzle position derivation from
/// tank center, turret aim direction, and weapon muzzle offset.
/// </summary>
/// <remarks>
/// Read-only inspection of <see cref="MatchSensorRuntimeState"/> and tank weapon loadouts.
/// Resolves <see cref="FixedVec2"/> muzzle position as
/// <see cref="TankState.Movement"/>.<see cref="MovementState.Position"/> plus
/// forward direction from <see cref="TankState.TurretRotation"/> scaled by
/// <see cref="WeaponDefinition.MuzzleOffsetFromCenter"/>. Does not use
/// <see cref="TankState.BodyRotation"/> as fallback. The
/// <see cref="FireMuzzlePositionStatus.MissingWeaponGeometry"/> branch is defensive when
/// offset is non-positive at resolve time; catalog <see cref="WeaponDefinition"/>
/// instances are constructed with positive offset. Does not construct
/// <see cref="MatchFireRequest"/>, allocate projectile ids, execute fire, mutate
/// match or sensor runtime state, log, replay, or integrate Godot.
/// </remarks>
public static class FireMuzzlePositionResolver
{
    public static FireMuzzlePositionResult Resolve(
        MatchSensorRuntimeState runtime,
        int tankIndex,
        WeaponSlot weaponSlot)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        MatchState state = runtime.State;
        int tankCount = state.Tanks.Count;

        if (tankIndex < 0 || tankIndex >= tankCount)
        {
            return FireMuzzlePositionResult.TankIndexOutOfRange();
        }

        TankState tank = state.Tanks[tankIndex];

        if (tank.IsDestroyed)
        {
            return FireMuzzlePositionResult.TankDestroyed();
        }

        TankWeaponLoadout loadout = state.Loadouts[tankIndex];

        if (weaponSlot.Value >= loadout.Weapons.Count)
        {
            return FireMuzzlePositionResult.WeaponSlotMissing();
        }

        WeaponState weapon = loadout.GetWeapon(weaponSlot);

        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(tank.TurretRotation);

        if (!forwardResult.IsResolved)
        {
            return FireMuzzlePositionResult.MissingAimDirection();
        }

        Fixed offset = weapon.Definition.MuzzleOffsetFromCenter;
        if (offset <= Fixed.Zero)
        {
            return FireMuzzlePositionResult.MissingWeaponGeometry();
        }

        FixedVec2 forward = forwardResult.Forward!.Value;
        FixedVec2 muzzlePosition = tank.Movement.Position + forward * offset;
        return FireMuzzlePositionResult.Resolved(muzzlePosition);
    }
}
