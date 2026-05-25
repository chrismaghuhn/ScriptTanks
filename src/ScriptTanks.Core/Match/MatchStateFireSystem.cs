using System;
using System.Linq;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure <see cref="MatchState"/>-level fire adapter. Applies exactly one
/// explicit fire request — shooter tank id, weapon slot, current tick,
/// projectile id, muzzle position, and fire velocity — by delegating to
/// <see cref="LoadoutFireResolver.Resolve(TankState, TankWeaponLoadout, WeaponSlot, SimTick, ProjectileId, FixedVec2, FixedVec2)"/>,
/// replacing the shooter's loadout, and (if firing succeeded) appending the
/// spawned projectile to the end of <see cref="MatchState.Projectiles"/>.
/// </summary>
/// <remarks>
/// <para>
/// The system performs first-match-by-id shooter lookup against
/// <see cref="MatchState.Tanks"/>. The matching index is reused for the
/// loadout because <see cref="MatchState"/> guarantees that the tank and
/// loadout collections have equal length. An unknown shooter id raises
/// <see cref="InvalidOperationException"/>; an out-of-range weapon slot is
/// not validated here and propagates from the loadout as
/// <see cref="ArgumentOutOfRangeException"/>.
/// </para>
/// <para>
/// Out of scope: AI/script execution, command queues, tank-resident fire
/// requests, tick advancement, projectile stepping/hit/cleanup,
/// match-runner integration, end-condition logic, tank movement, aiming or
/// muzzle calculation, projectile-id uniqueness, fire-velocity magnitude
/// validation, friendly-fire / line-of-sight / arena-bounds checks, combat
/// logs, replay frames, serialization, and Godot integration.
/// </para>
/// </remarks>
public static class MatchStateFireSystem
{
    /// <summary>
    /// Convenience overload that unpacks a <see cref="MatchFireRequest"/> and
    /// delegates to the long-parameter
    /// <see cref="ResolveFire(MatchState, TankId, WeaponSlot, SimTick, ProjectileId, FixedVec2, FixedVec2)"/>.
    /// Behaves identically to the long-parameter overload, including all
    /// validation and exception semantics; null <paramref name="state"/>
    /// raises <see cref="ArgumentNullException"/> from the delegated method.
    /// </summary>
    public static MatchStateFireOutcome ResolveFire(
        MatchState state,
        MatchFireRequest request,
        SimTick currentTick)
        => ResolveFire(
            state,
            request.ShooterTankId,
            request.WeaponSlot,
            currentTick,
            request.ProjectileId,
            request.MuzzlePosition,
            request.FireVelocity);

    public static MatchStateFireOutcome ResolveFire(
        MatchState state,
        TankId shooterTankId,
        WeaponSlot weaponSlot,
        SimTick currentTick,
        ProjectileId projectileId,
        FixedVec2 muzzlePosition,
        FixedVec2 fireVelocity)
    {
        ArgumentNullException.ThrowIfNull(state);

        int shooterIndex = -1;
        for (int i = 0; i < state.Tanks.Count; i++)
        {
            if (state.Tanks[i].Id == shooterTankId)
            {
                shooterIndex = i;
                break;
            }
        }

        if (shooterIndex < 0)
        {
            throw new InvalidOperationException(
                $"No tank with id '{shooterTankId}' exists in the current match state.");
        }

        TankState shooter = state.Tanks[shooterIndex];
        TankWeaponLoadout shooterLoadout = state.Loadouts[shooterIndex];

        LoadoutFireResolutionOutcome fireOutcome = LoadoutFireResolver.Resolve(
            shooter,
            shooterLoadout,
            weaponSlot,
            currentTick,
            projectileId,
            muzzlePosition,
            fireVelocity);

        TankWeaponLoadout[] updatedLoadouts = state.Loadouts.ToArray();
        updatedLoadouts[shooterIndex] = fireOutcome.UpdatedLoadout;

        MatchState afterLoadouts = state.WithLoadouts(updatedLoadouts);

        if (fireOutcome.DidFire)
        {
            ProjectileState spawnedProjectile = fireOutcome.SpawnedProjectile!.Value;
            ProjectileState[] updatedProjectiles = state.Projectiles
                .Concat(new[] { spawnedProjectile })
                .ToArray();
            MatchState afterProjectiles = afterLoadouts.WithProjectiles(updatedProjectiles);
            return MatchStateFireOutcome.Fired(afterProjectiles, spawnedProjectile);
        }

        return MatchStateFireOutcome.NotReady(afterLoadouts);
    }
}
