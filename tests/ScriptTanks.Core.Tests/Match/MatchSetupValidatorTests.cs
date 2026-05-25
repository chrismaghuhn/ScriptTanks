using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Geometry;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchSetupValidatorTests
{
    private static TankState CreateTank(
        int tankId,
        int ownerSlot,
        FixedVec2 position,
        TankDefinition? definition = null,
        int? hp = null)
    {
        TankDefinition tankDefinition = definition ?? TankCatalog.BasicTank;
        MovementState movement = new MovementState(position, FixedVec2.Zero);
        return new TankState(
            new TankId(tankId),
            new PlayerSlot(ownerSlot),
            tankDefinition,
            movement,
            hp ?? tankDefinition.Stats.MaxHitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static MatchInitialState CreateMatch(params TankState[] tanks)
        => new MatchInitialState(ArenaCatalog.OpenTestArena, tanks);

    private static MatchInitialState CreateObstacleMatch(params TankState[] tanks)
        => new MatchInitialState(ArenaCatalog.ObstacleTestArena, tanks);

    private static ArenaDefinition CreateArenaWithTwoWalls()
    {
        return new ArenaDefinition(
            id: "two_wall_test_arena",
            displayName: "Two Wall Test Arena",
            description: "Test arena with two overlapping wall checks.",
            bounds: new ArenaBounds(Fixed.FromInt(100), Fixed.FromInt(60)),
            startPositions: new[]
            {
                new ArenaStartPosition(new PlayerSlot(0), FixedVec2.FromInts(50, 30)),
            },
            wallBlocks: new[]
            {
                new WallBlock(
                    "wall_a",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(48, 28),
                        Fixed.FromInt(4),
                        Fixed.FromInt(4))),
                new WallBlock(
                    "wall_b",
                    FixedRect.FromMinSize(
                        FixedVec2.FromInts(49, 29),
                        Fixed.FromInt(4),
                        Fixed.FromInt(4))),
            },
            patrolPoints: Array.Empty<FixedVec2>(),
            tags: new[] { "test" });
    }

    // -------------------- Block A: MatchSetupValidationResult --------------------

    [Fact]
    public void Result_Constructor_RejectsNullIssues()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MatchSetupValidationResult(null!));
    }

    [Fact]
    public void Result_Constructor_DefensivelyCopiesIssues()
    {
        List<MatchSetupValidationIssue> source = new List<MatchSetupValidationIssue>
        {
            MatchSetupValidationIssue.DuplicateTankId,
        };

        MatchSetupValidationResult result = new MatchSetupValidationResult(source);
        source.Add(MatchSetupValidationIssue.DuplicateOwnerSlot);

        Assert.Single(result.Issues);
        Assert.Equal(MatchSetupValidationIssue.DuplicateTankId, result.Issues[0]);
    }

    [Fact]
    public void Result_IsValid_IsTrue_WhenIssuesIsEmpty()
    {
        MatchSetupValidationResult result =
            new MatchSetupValidationResult(Array.Empty<MatchSetupValidationIssue>());

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Result_IsValid_IsFalse_WhenIssuesIsNotEmpty()
    {
        MatchSetupValidationResult result = new MatchSetupValidationResult(
            new[] { MatchSetupValidationIssue.TankDestroyedAtStart });

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void Result_Constructor_PreservesIssueOrder()
    {
        MatchSetupValidationIssue[] expected = new[]
        {
            MatchSetupValidationIssue.DuplicateTankId,
            MatchSetupValidationIssue.TankDestroyedAtStart,
            MatchSetupValidationIssue.DuplicateTankId,
            MatchSetupValidationIssue.TankHitboxIntersectsWall,
        };

        MatchSetupValidationResult result = new MatchSetupValidationResult(expected);

        Assert.Equal(expected, result.Issues.ToArray());
    }

    // -------------------- Block B: Null Input --------------------

    [Fact]
    public void Validate_RejectsNullInitialState()
    {
        Assert.Throws<ArgumentNullException>(
            () => MatchSetupValidator.Validate(null!));
    }

    // -------------------- Block C: Valid Setup --------------------

    [Fact]
    public void Validate_ReturnsValid_ForOpenArenaTwoValidTanks()
    {
        TankState tankA = CreateTank(0, 0, FixedVec2.FromInts(10, 30));
        TankState tankB = CreateTank(1, 1, FixedVec2.FromInts(90, 30));
        MatchInitialState match = CreateMatch(tankA, tankB);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    // -------------------- Block D: Duplicate Tank IDs --------------------

    [Fact]
    public void Validate_ReportsDuplicateTankId()
    {
        TankState tankA = CreateTank(0, 0, FixedVec2.FromInts(10, 30));
        TankState tankB = CreateTank(0, 1, FixedVec2.FromInts(90, 30));
        MatchInitialState match = CreateMatch(tankA, tankB);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.False(result.IsValid);
        Assert.Equal(
            new[] { MatchSetupValidationIssue.DuplicateTankId },
            result.Issues.ToArray());
    }

    [Fact]
    public void Validate_ReportsDuplicateTankId_PerDuplicateAfterFirst()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 10));
        TankState tank1 = CreateTank(0, 1, FixedVec2.FromInts(50, 30));
        TankState tank2 = CreateTank(0, 2, FixedVec2.FromInts(90, 50));
        MatchInitialState match = CreateMatch(tank0, tank1, tank2);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[]
            {
                MatchSetupValidationIssue.DuplicateTankId,
                MatchSetupValidationIssue.DuplicateTankId,
            },
            result.Issues.ToArray());
    }

    // -------------------- Block E: Duplicate Owner Slots --------------------

    [Fact]
    public void Validate_ReportsDuplicateOwnerSlot()
    {
        TankState tankA = CreateTank(0, 0, FixedVec2.FromInts(10, 30));
        TankState tankB = CreateTank(1, 0, FixedVec2.FromInts(90, 30));
        MatchInitialState match = CreateMatch(tankA, tankB);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[] { MatchSetupValidationIssue.DuplicateOwnerSlot },
            result.Issues.ToArray());
    }

    [Fact]
    public void Validate_ReportsDuplicateOwnerSlot_PerDuplicateAfterFirst()
    {
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(10, 10));
        TankState tank1 = CreateTank(1, 0, FixedVec2.FromInts(50, 30));
        TankState tank2 = CreateTank(2, 0, FixedVec2.FromInts(90, 50));
        MatchInitialState match = CreateMatch(tank0, tank1, tank2);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[]
            {
                MatchSetupValidationIssue.DuplicateOwnerSlot,
                MatchSetupValidationIssue.DuplicateOwnerSlot,
            },
            result.Issues.ToArray());
    }

    // -------------------- Block F: Destroyed Tank --------------------

    [Fact]
    public void Validate_ReportsTankDestroyedAtStart()
    {
        TankState destroyed = CreateTank(0, 0, FixedVec2.FromInts(10, 30), hp: 0);
        MatchInitialState match = CreateMatch(destroyed);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[] { MatchSetupValidationIssue.TankDestroyedAtStart },
            result.Issues.ToArray());
    }

    // -------------------- Block G: Bounds --------------------

    [Fact]
    public void Validate_ReportsTankPositionOutsideArenaBounds_AndHitboxOutside()
    {
        TankState tank = CreateTank(
            0,
            0,
            new FixedVec2(Fixed.FromInt(-1), Fixed.FromInt(30)));
        MatchInitialState match = CreateMatch(tank);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[]
            {
                MatchSetupValidationIssue.TankPositionOutsideArenaBounds,
                MatchSetupValidationIssue.TankHitboxOutsideArenaBounds,
            },
            result.Issues.ToArray());
    }

    [Fact]
    public void Validate_ReportsTankHitboxOutsideArenaBounds_WhenCenterInside()
    {
        TankState tank = CreateTank(0, 0, FixedVec2.FromInts(1, 30));
        MatchInitialState match = CreateMatch(tank);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[] { MatchSetupValidationIssue.TankHitboxOutsideArenaBounds },
            result.Issues.ToArray());
    }

    // -------------------- Block H: Walls --------------------

    [Fact]
    public void Validate_ReportsTankHitboxIntersectsWall()
    {
        TankState tank = CreateTank(0, 0, FixedVec2.FromInts(45, 25));
        MatchInitialState match = CreateObstacleMatch(tank);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[] { MatchSetupValidationIssue.TankHitboxIntersectsWall },
            result.Issues.ToArray());
    }

    [Fact]
    public void Validate_ReportsTankHitboxIntersectsWall_OnlyOncePerTank()
    {
        ArenaDefinition arena = CreateArenaWithTwoWalls();
        TankState tank = CreateTank(0, 0, FixedVec2.FromInts(50, 30));
        MatchInitialState match = new MatchInitialState(arena, new[] { tank });

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[] { MatchSetupValidationIssue.TankHitboxIntersectsWall },
            result.Issues.ToArray());
    }

    // -------------------- Block I: Issue Ordering --------------------

    [Fact]
    public void Validate_ReportsIssuesInDeterministicOrder()
    {
        TankState tankA = CreateTank(0, 0, FixedVec2.FromInts(10, 30));
        TankState tankB = CreateTank(
            0,
            0,
            new FixedVec2(Fixed.FromInt(-10), Fixed.FromInt(30)),
            hp: 0);
        MatchInitialState match = CreateMatch(tankA, tankB);

        MatchSetupValidationResult result = MatchSetupValidator.Validate(match);

        Assert.Equal(
            new[]
            {
                MatchSetupValidationIssue.DuplicateTankId,
                MatchSetupValidationIssue.DuplicateOwnerSlot,
                MatchSetupValidationIssue.TankDestroyedAtStart,
                MatchSetupValidationIssue.TankPositionOutsideArenaBounds,
                MatchSetupValidationIssue.TankHitboxOutsideArenaBounds,
            },
            result.Issues.ToArray());
    }
}
