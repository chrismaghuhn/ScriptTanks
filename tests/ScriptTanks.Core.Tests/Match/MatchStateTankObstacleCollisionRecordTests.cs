using System;
using System.Reflection;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankObstacleCollisionRecordTests
{
    private static FixedVec2 SampleInitial()
        => FixedVec2.FromInts(10, 20);

    private static FixedVec2 SampleCandidate()
        => FixedVec2.FromInts(15, 20);

    [Fact]
    public void Constructor_negative_tank_index_throws()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                -1,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.Unchanged,
                initial,
                candidate,
                candidate,
                blockingWallBlockId: null));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_undefined_status_throws()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                (MatchStateTankObstacleCollisionStatus)99,
                initial,
                candidate,
                candidate,
                blockingWallBlockId: null));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Constructor_blocked_by_obstacle_requires_blocking_wall_id()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.BlockedByObstacle,
                initial,
                candidate,
                initial,
                blockingWallBlockId: null));

        Assert.Equal("blockingWallBlockId", ex.ParamName);
    }

    [Fact]
    public void Constructor_started_inside_obstacle_requires_blocking_wall_id()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.StartedInsideObstacle,
                initial,
                candidate,
                initial,
                blockingWallBlockId: "   "));

        Assert.Equal("blockingWallBlockId", ex.ParamName);
    }

    [Fact]
    public void Constructor_unchanged_rejects_blocking_wall_id()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.Unchanged,
                initial,
                candidate,
                candidate,
                blockingWallBlockId: "wall_a"));

        Assert.Equal("blockingWallBlockId", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_destroyed_rejects_blocking_wall_id()
    {
        FixedVec2 position = SampleInitial();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.SkippedDestroyed,
                position,
                position,
                position,
                blockingWallBlockId: "wall_a"));

        Assert.Equal("blockingWallBlockId", ex.ParamName);
    }

    [Fact]
    public void Constructor_unchanged_requires_final_equals_candidate()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.Unchanged,
                initial,
                candidate,
                initial,
                blockingWallBlockId: null));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Constructor_blocked_by_obstacle_requires_final_equals_initial()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.BlockedByObstacle,
                initial,
                candidate,
                candidate,
                blockingWallBlockId: "wall_a"));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Constructor_skipped_destroyed_requires_final_equals_candidate()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.SkippedDestroyed,
                initial,
                candidate,
                initial,
                blockingWallBlockId: null));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Constructor_started_inside_obstacle_requires_final_equals_initial()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionRecord(
                0,
                new TankId(0),
                MatchStateTankObstacleCollisionStatus.StartedInsideObstacle,
                initial,
                candidate,
                candidate,
                blockingWallBlockId: "wall_a"));

        Assert.Equal("finalPosition", ex.ParamName);
    }

    [Fact]
    public void Unchanged_factory_sets_status_positions_and_DidBlockMovement_false()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        MatchStateTankObstacleCollisionRecord record =
            MatchStateTankObstacleCollisionRecord.Unchanged(0, new TankId(0), initial, candidate);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.Unchanged, record.Status);
        Assert.Equal(initial, record.InitialPosition);
        Assert.Equal(candidate, record.CandidatePosition);
        Assert.Equal(candidate, record.FinalPosition);
        Assert.Null(record.BlockingWallBlockId);
        Assert.False(record.DidBlockMovement);
    }

    [Fact]
    public void BlockedByObstacle_factory_sets_status_final_initial_and_DidBlockMovement_true()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();
        const string wallId = "center_wall";

        MatchStateTankObstacleCollisionRecord record =
            MatchStateTankObstacleCollisionRecord.BlockedByObstacle(
                1,
                new TankId(1),
                initial,
                candidate,
                wallId);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.BlockedByObstacle, record.Status);
        Assert.Equal(initial, record.InitialPosition);
        Assert.Equal(candidate, record.CandidatePosition);
        Assert.Equal(initial, record.FinalPosition);
        Assert.Equal(wallId, record.BlockingWallBlockId);
        Assert.True(record.DidBlockMovement);
    }

    [Fact]
    public void SkippedDestroyed_factory_sets_equal_positions_and_DidBlockMovement_false()
    {
        FixedVec2 position = SampleInitial();

        MatchStateTankObstacleCollisionRecord record =
            MatchStateTankObstacleCollisionRecord.SkippedDestroyed(2, new TankId(2), position);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.SkippedDestroyed, record.Status);
        Assert.Equal(position, record.InitialPosition);
        Assert.Equal(position, record.CandidatePosition);
        Assert.Equal(position, record.FinalPosition);
        Assert.Null(record.BlockingWallBlockId);
        Assert.False(record.DidBlockMovement);
    }

    [Fact]
    public void StartedInsideObstacle_factory_sets_status_final_initial_and_DidBlockMovement_true()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();
        const string wallId = "spawn_wall";

        MatchStateTankObstacleCollisionRecord record =
            MatchStateTankObstacleCollisionRecord.StartedInsideObstacle(
                0,
                new TankId(0),
                initial,
                candidate,
                wallId);

        Assert.Equal(MatchStateTankObstacleCollisionStatus.StartedInsideObstacle, record.Status);
        Assert.Equal(initial, record.FinalPosition);
        Assert.Equal(wallId, record.BlockingWallBlockId);
        Assert.True(record.DidBlockMovement);
    }

    [Fact]
    public void Separate_instances_with_same_values_are_not_same_reference()
    {
        FixedVec2 initial = SampleInitial();
        FixedVec2 candidate = SampleCandidate();

        MatchStateTankObstacleCollisionRecord first =
            MatchStateTankObstacleCollisionRecord.Unchanged(0, new TankId(0), initial, candidate);
        MatchStateTankObstacleCollisionRecord second =
            MatchStateTankObstacleCollisionRecord.Unchanged(0, new TankId(0), initial, candidate);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equalsMethod = typeof(MatchStateTankObstacleCollisionRecord).GetMethod(
            nameof(object.Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equalsMethod);
        Assert.Equal(typeof(object), equalsMethod!.DeclaringType);
    }
}
