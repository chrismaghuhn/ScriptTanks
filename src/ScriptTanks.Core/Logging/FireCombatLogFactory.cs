using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Simulation;

namespace ScriptTanks.Core.Logging;

/// <summary>
/// Pure factory that converts one explicit fire request and its outcome into
/// an immutable <see cref="CombatLog"/> snapshot.
/// </summary>
/// <remarks>
/// This type only converts data. It does not emit logs, store logs globally,
/// integrate with the fire system, tick pipeline, runner, replay recorder,
/// serialization, UI, or Godot. The supplied <see cref="SimTick"/> is used
/// for every generated entry; same-tick entries are valid in
/// <see cref="CombatLog"/>. Messages are concrete human-readable strings
/// built from the request and outcome IDs - no template formatter is used
/// or introduced here.
/// </remarks>
public static class FireCombatLogFactory
{
    public static CombatLog CreateFireLog(
        SimTick tick,
        MatchFireRequest request,
        MatchStateFireOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            tick,
            CombatLogCategory.Weapon,
            CombatLogEventTypes.FireRequested,
            $"Tank {request.ShooterTankId} requested fire from weapon slot {request.WeaponSlot}.");

        if (outcome.DidFire)
        {
            builder.Add(
                tick,
                CombatLogCategory.Weapon,
                CombatLogEventTypes.FireSucceeded,
                $"Tank {request.ShooterTankId} fired weapon slot {request.WeaponSlot}.");

            builder.Add(
                tick,
                CombatLogCategory.Projectile,
                CombatLogEventTypes.ProjectileSpawned,
                $"Projectile {outcome.SpawnedProjectile!.Value.Id} spawned from tank {request.ShooterTankId}.");
        }
        else
        {
            builder.Add(
                tick,
                CombatLogCategory.Weapon,
                CombatLogEventTypes.FireNotReady,
                $"Tank {request.ShooterTankId} could not fire weapon slot {request.WeaponSlot}: weapon not ready.");
        }

        return builder.Build();
    }
}
