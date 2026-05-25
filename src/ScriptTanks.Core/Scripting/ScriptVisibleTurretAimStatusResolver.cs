using System;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Scripting;

/// <summary>
/// Pure computed-on-demand resolver for script-visible turret aim and alignment status.
/// </summary>
/// <remarks>
/// Reads <see cref="MatchState"/> and a tank index only. Target selection mirrors
/// <see cref="ScriptMappedTurretRequestApplicationPipeline"/> nearest-alive-enemy policy
/// (position-based <see cref="FixedVec2.DistanceSquaredTo"/>; no sensor range or
/// <see cref="ScriptEvaluationContext.EnemyVisible"/>). Does not apply turret rotation,
/// evaluate script conditions, gate fire, mutate match state, log, replay, or integrate Godot.
/// Keep target-selection logic in sync with the turret pipeline until a shared helper exists.
/// </remarks>
public static class ScriptVisibleTurretAimStatusResolver
{
    public static ScriptVisibleTurretAimStatusResult Resolve(MatchState state, int tankIndex)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (tankIndex < 0 || tankIndex >= state.Tanks.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tankIndex),
                tankIndex,
                "Tank index is out of range.");
        }

        TankState owner = state.Tanks[tankIndex];

        if (owner.IsDestroyed)
        {
            return ScriptVisibleTurretAimStatusResult.OwnerDestroyed(
                owner.Id,
                tankIndex,
                owner.TurretRotation);
        }

        if (!TrySelectNearestAliveEnemy(state, tankIndex, out int targetTankIndex))
        {
            return ScriptVisibleTurretAimStatusResult.NoTarget(
                owner.Id,
                tankIndex,
                owner.TurretRotation);
        }

        TankState target = state.Tanks[targetTankIndex];
        FixedVec2 delta = target.Movement.Position - owner.Movement.Position;
        FixedRotationAimResolution aim = FixedRotationAimResolver.ResolveFromDirection(delta);

        if (!aim.IsResolved)
        {
            return ScriptVisibleTurretAimStatusResult.MissingAimSolution(
                owner.Id,
                tankIndex,
                target.Id,
                targetTankIndex,
                owner.TurretRotation);
        }

        Fixed desiredRotation = aim.Rotation;
        FixedRotationTurnStepResult step = FixedRotationTurnStepResolver.ResolveStep(
            owner.TurretRotation,
            desiredRotation,
            Fixed.One);

        if (step.CurrentRotation == step.DesiredRotation)
        {
            return ScriptVisibleTurretAimStatusResult.Aligned(
                owner.Id,
                tankIndex,
                target.Id,
                targetTankIndex,
                step.CurrentRotation,
                step.DesiredRotation);
        }

        return ScriptVisibleTurretAimStatusResult.Turning(
            owner.Id,
            tankIndex,
            target.Id,
            targetTankIndex,
            step.CurrentRotation,
            step.DesiredRotation,
            step.AppliedTurnDelta,
            step.AppliedTurnMagnitude);
    }

    private static bool TrySelectNearestAliveEnemy(
        MatchState state,
        int ownerTankIndex,
        out int targetTankIndex)
    {
        targetTankIndex = -1;
        Fixed? bestDistanceSquared = null;

        TankState owner = state.Tanks[ownerTankIndex];
        FixedVec2 ownerPosition = owner.Movement.Position;

        for (int candidateIndex = 0; candidateIndex < state.Tanks.Count; candidateIndex++)
        {
            if (candidateIndex == ownerTankIndex)
            {
                continue;
            }

            TankState candidate = state.Tanks[candidateIndex];

            if (candidate.IsDestroyed)
            {
                continue;
            }

            Fixed distanceSquared =
                ownerPosition.DistanceSquaredTo(candidate.Movement.Position);

            if (!bestDistanceSquared.HasValue
                || distanceSquared < bestDistanceSquared.Value
                || (distanceSquared == bestDistanceSquared.Value
                    && candidateIndex < targetTankIndex))
            {
                bestDistanceSquared = distanceSquared;
                targetTankIndex = candidateIndex;
            }
        }

        return targetTankIndex >= 0;
    }
}
