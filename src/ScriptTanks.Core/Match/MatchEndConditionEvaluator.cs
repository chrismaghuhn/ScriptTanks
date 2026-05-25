using System;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Pure end-condition evaluator for a <see cref="MatchState"/> snapshot.
/// Classifies the snapshot as still running, ended by tank destruction,
/// ended by timeout HP advantage, or ended by timeout draw, given a
/// <c>maxTicks</c> tick budget.
/// </summary>
/// <remarks>
/// <para>
/// The evaluator is a read-only query: it never mutates state, never runs
/// gameplay systems, never advances ticks, and never produces side effects.
/// Destruction is checked first; the timeout rule is checked second.
/// At timeout the highest <see cref="TankState.CurrentHitPoints"/> across
/// all tanks (including destroyed ones) is compared and a unique leader
/// wins, otherwise the result is a draw.
/// </para>
/// <para>
/// Out of scope: <c>MatchRunner</c>, match loop composition, projectile
/// movement, fire resolution, tank movement, AI/script execution, team
/// logic, ranking logic, combat logs, replay frames, serialization, and
/// Godot integration.
/// </para>
/// </remarks>
public static class MatchEndConditionEvaluator
{
    public static MatchEndConditionResult Evaluate(MatchState state, int maxTicks)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (maxTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTicks),
                maxTicks,
                "maxTicks must not be negative.");
        }

        TankState soleSurvivor = default;
        int aliveCount = 0;
        int destroyedCount = 0;
        foreach (TankState tank in state.Tanks)
        {
            if (tank.IsDestroyed)
            {
                destroyedCount++;
            }
            else
            {
                aliveCount++;
                soleSurvivor = tank;
            }
        }

        if (aliveCount == 1 && destroyedCount >= 1)
        {
            return MatchEndConditionResult.TankDestroyed(soleSurvivor);
        }

        if (state.CurrentTick.Value >= maxTicks)
        {
            int highest = int.MinValue;
            int highestCount = 0;
            TankState topCandidate = default;
            foreach (TankState tank in state.Tanks)
            {
                if (tank.CurrentHitPoints > highest)
                {
                    highest = tank.CurrentHitPoints;
                    highestCount = 1;
                    topCandidate = tank;
                }
                else if (tank.CurrentHitPoints == highest)
                {
                    highestCount++;
                }
            }

            return highestCount == 1
                ? MatchEndConditionResult.TimeoutHpAdvantage(topCandidate)
                : MatchEndConditionResult.TimeoutDraw();
        }

        return MatchEndConditionResult.Running();
    }
}
