using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;

namespace ScriptTanks.Core.Match;

/// <summary>
/// Deterministic, non-throwing match-setup validator. Inspects a
/// <see cref="MatchInitialState"/> and returns a
/// <see cref="MatchSetupValidationResult"/> describing every detected
/// gameplay-setup issue. Only a <c>null</c> initial state throws — every
/// other gameplay-rule violation is reported as a
/// <see cref="MatchSetupValidationIssue"/> so that UI, debug overlays,
/// and combat coaches can render them without exception handling.
/// <para>
/// For each tank in <see cref="MatchInitialState.Tanks"/> the validator
/// evaluates the following checks in this fixed order:
/// </para>
/// <list type="number">
/// <item><description><see cref="MatchSetupValidationIssue.DuplicateTankId"/></description></item>
/// <item><description><see cref="MatchSetupValidationIssue.DuplicateOwnerSlot"/></description></item>
/// <item><description><see cref="MatchSetupValidationIssue.TankDestroyedAtStart"/></description></item>
/// <item><description><see cref="MatchSetupValidationIssue.TankPositionOutsideArenaBounds"/></description></item>
/// <item><description><see cref="MatchSetupValidationIssue.TankHitboxOutsideArenaBounds"/></description></item>
/// <item><description><see cref="MatchSetupValidationIssue.TankHitboxIntersectsWall"/></description></item>
/// </list>
/// <para>
/// Duplicates use sets seeded in iteration order: the first occurrence is
/// silent, every later occurrence appends one issue. Hitbox-out-of-bounds
/// is independent from the center-in-bounds check; both can fire for the
/// same tank. Wall intersection is reported at most once per tank, even
/// if multiple walls overlap the same hitbox.
/// </para>
/// </summary>
public static class MatchSetupValidator
{
    public static MatchSetupValidationResult Validate(MatchInitialState initialState)
    {
        ArgumentNullException.ThrowIfNull(initialState);

        List<MatchSetupValidationIssue> issues = new List<MatchSetupValidationIssue>();
        HashSet<TankId> seenTankIds = new HashSet<TankId>();
        HashSet<PlayerSlot> seenOwnerSlots = new HashSet<PlayerSlot>();

        foreach (TankState tank in initialState.Tanks)
        {
            if (!seenTankIds.Add(tank.Id))
            {
                issues.Add(MatchSetupValidationIssue.DuplicateTankId);
            }

            if (!seenOwnerSlots.Add(tank.OwnerSlot))
            {
                issues.Add(MatchSetupValidationIssue.DuplicateOwnerSlot);
            }

            if (tank.IsDestroyed)
            {
                issues.Add(MatchSetupValidationIssue.TankDestroyedAtStart);
            }

            if (!initialState.Arena.Bounds.Contains(tank.Movement.Position))
            {
                issues.Add(MatchSetupValidationIssue.TankPositionOutsideArenaBounds);
            }

            if (!IsTankHitboxFullyInsideArena(initialState, tank))
            {
                issues.Add(MatchSetupValidationIssue.TankHitboxOutsideArenaBounds);
            }

            if (DoesTankHitboxIntersectAnyWall(initialState, tank))
            {
                issues.Add(MatchSetupValidationIssue.TankHitboxIntersectsWall);
            }
        }

        return new MatchSetupValidationResult(issues);
    }

    private static bool IsTankHitboxFullyInsideArena(MatchInitialState state, TankState tank)
    {
        Fixed radius = tank.Definition.Stats.HitboxRadius;
        FixedVec2 center = tank.Movement.Position;
        Fixed width = state.Arena.Bounds.Width;
        Fixed height = state.Arena.Bounds.Height;

        return center.X - radius >= Fixed.Zero
            && center.Y - radius >= Fixed.Zero
            && center.X + radius <= width
            && center.Y + radius <= height;
    }

    private static bool DoesTankHitboxIntersectAnyWall(MatchInitialState state, TankState tank)
    {
        CircleShape circle = new CircleShape(
            tank.Movement.Position,
            tank.Definition.Stats.HitboxRadius);

        foreach (WallBlock wall in state.Arena.WallBlocks)
        {
            if (CollisionChecks.CircleIntersectsRect(circle, wall.Bounds))
            {
                return true;
            }
        }

        return false;
    }
}
