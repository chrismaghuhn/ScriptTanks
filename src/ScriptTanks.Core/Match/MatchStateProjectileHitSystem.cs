using System;
using System.Linq;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure match-state system that detects and resolves projectile hits against
/// arena walls and current tank snapshots from <see cref="MatchState"/>.
/// Updates projectile states and replaces hit tanks by <see cref="TankId"/>
/// in the tank array while preserving tank order and count. This system does
/// not mutate the input <see cref="MatchState"/>, advance
/// <see cref="MatchState.CurrentTick"/>, move projectiles, resolve fire
/// attempts, remove inactive projectiles, mutate loadouts, create combat logs
/// or replay frames, or determine winners. On the spawn tick only, the
/// owner tank is excluded from hit candidates when
/// <c>projectile.SpawnTick == state.CurrentTick</c>. Other team/self-hit
/// policy remains caller or pipeline concern.
/// </summary>
public static class MatchStateProjectileHitSystem
{
    public static MatchState ResolveProjectileHits(MatchState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        TankState[] updatedTanks = state.Tanks.ToArray();
        ProjectileState[] updatedProjectiles = state.Projectiles.ToArray();

        for (int i = 0; i < updatedProjectiles.Length; i++)
        {
            ProjectileState projectile = updatedProjectiles[i];

            IEnumerable<TankState> tankCandidates = updatedTanks;
            if (projectile.SpawnTick == state.CurrentTick)
            {
                tankCandidates = updatedTanks.Where(t => !t.Id.Equals(projectile.OwnerTankId));
            }

            ProjectileHitResult hit = ProjectileHitDetection.Detect(
                projectile,
                state.Arena,
                tankCandidates);

            ProjectileHitResolutionOutcome outcome = ProjectileHitResolution.Resolve(
                projectile,
                hit,
                updatedTanks);

            updatedProjectiles[i] = outcome.UpdatedProjectile;

            if (outcome.HasUpdatedTank)
            {
                ReplaceFirstMatchingTank(updatedTanks, outcome.UpdatedTank!.Value);
            }
        }

        return new MatchState(
            state.Arena,
            state.CurrentTick,
            updatedTanks,
            state.Loadouts,
            updatedProjectiles);
    }

    private static void ReplaceFirstMatchingTank(TankState[] tanks, TankState updatedTank)
    {
        for (int i = 0; i < tanks.Length; i++)
        {
            if (tanks[i].Id.Equals(updatedTank.Id))
            {
                tanks[i] = updatedTank;
                return;
            }
        }

        throw new InvalidOperationException(
            "Projectile hit resolution returned a tank that does not exist in the match state.");
    }
}
