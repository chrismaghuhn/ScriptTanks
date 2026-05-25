using System;
using System.Collections.Generic;
using System.Reflection;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Match;

public sealed class MatchStateTankObstacleCollisionResultTests
{
    private static MovementState CreateMovement(FixedVec2 position, FixedVec2 velocity)
        => new MovementState(position, velocity);

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position,
        FixedVec2 velocity,
        int hitPoints)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            CreateMovement(position, velocity),
            hitPoints,
            Fixed.Zero,
            Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static MatchState CreateState(params TankState[] tanks)
    {
        TankWeaponLoadout[] loadouts = new TankWeaponLoadout[tanks.Length];
        for (int i = 0; i < tanks.Length; i++)
        {
            loadouts[i] = CreateLoadout();
        }

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(5),
            tanks,
            loadouts,
            Array.Empty<ProjectileState>());
    }

    private static MatchStateTankObstacleCollisionRecord[] CreateValidRecords(
        MatchState preMovementState,
        MatchState candidateState,
        MatchState finalState)
    {
        var records = new MatchStateTankObstacleCollisionRecord[preMovementState.Tanks.Count];

        for (int i = 0; i < records.Length; i++)
        {
            FixedVec2 initial = preMovementState.Tanks[i].Movement.Position;
            FixedVec2 candidate = candidateState.Tanks[i].Movement.Position;
            FixedVec2 final = finalState.Tanks[i].Movement.Position;

            records[i] = initial.Equals(candidate) && candidate.Equals(final)
                ? MatchStateTankObstacleCollisionRecord.Unchanged(
                    i,
                    preMovementState.Tanks[i].Id,
                    initial,
                    candidate)
                : initial.Equals(final)
                    ? MatchStateTankObstacleCollisionRecord.BlockedByObstacle(
                        i,
                        preMovementState.Tanks[i].Id,
                        initial,
                        candidate,
                        "wall_a")
                    : MatchStateTankObstacleCollisionRecord.Unchanged(
                        i,
                        preMovementState.Tanks[i].Id,
                        initial,
                        final);
        }

        return records;
    }

    [Fact]
    public void Constructor_preserves_state_references()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(15, 20), FixedVec2.FromInts(1, 0), 100));
        MatchState final = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.FromInts(1, 0), 100));

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            CreateValidRecords(pre, candidate, final));

        Assert.Same(pre, result.PreMovementState);
        Assert.Same(candidate, result.CandidateState);
        Assert.Same(final, result.FinalState);
    }

    [Fact]
    public void Constructor_sets_count()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState candidate = pre;
        MatchState final = pre;

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            CreateValidRecords(pre, candidate, final));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Records_are_read_only()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = pre;
        MatchState final = pre;

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            CreateValidRecords(pre, candidate, final));

        Assert.Throws<NotSupportedException>(() =>
            ((IList<MatchStateTankObstacleCollisionRecord>)result.Records)[0] =
                MatchStateTankObstacleCollisionRecord.Unchanged(
                    0,
                    new TankId(0),
                    pre.Tanks[0].Movement.Position,
                    pre.Tanks[0].Movement.Position));
    }

    [Fact]
    public void Constructor_defensively_copies_records()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = pre;
        MatchState final = pre;
        MatchStateTankObstacleCollisionRecord[] records = CreateValidRecords(pre, candidate, final);

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            records);

        records[0] = MatchStateTankObstacleCollisionRecord.BlockedByObstacle(
            0,
            new TankId(0),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(99, 99),
            "other");

        Assert.Equal(MatchStateTankObstacleCollisionStatus.Unchanged, result.GetRecordAtIndex(0).Status);
    }

    [Fact]
    public void GetRecordAtIndex_returns_record_in_order()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState candidate = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(35, 20), FixedVec2.FromInts(1, 0), 100));
        MatchState final = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.FromInts(1, 0), 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                candidate.Tanks[0].Movement.Position),
            MatchStateTankObstacleCollisionRecord.BlockedByObstacle(
                1,
                new TankId(1),
                pre.Tanks[1].Movement.Position,
                candidate.Tanks[1].Movement.Position,
                "wall_b"),
        };

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            records);

        Assert.Same(records[0], result.GetRecordAtIndex(0));
        Assert.Same(records[1], result.GetRecordAtIndex(1));
        Assert.True(result.GetRecordAtIndex(1).DidBlockMovement);
    }

    [Fact]
    public void Constructor_null_preMovementState_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankObstacleCollisionResult(
                null!,
                state,
                state,
                CreateValidRecords(state, state, state)));

        Assert.Equal("preMovementState", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_candidateState_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankObstacleCollisionResult(
                state,
                null!,
                state,
                CreateValidRecords(state, state, state)));

        Assert.Equal("candidateState", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_finalState_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankObstacleCollisionResult(
                state,
                state,
                null!,
                CreateValidRecords(state, state, state)));

        Assert.Equal("finalState", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_records_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankObstacleCollisionResult(state, state, state, null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_record_element_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records = CreateValidRecords(state, state, state);
        records[1] = null!;

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(state, state, state, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_count_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                pre.Tanks[0].Movement.Position),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, pre, pre, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_candidate_state_tank_count_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState candidate = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(
                pre,
                candidate,
                pre,
                CreateValidRecords(pre, pre, pre)));

        Assert.Equal("candidateState", ex.ParamName);
    }

    [Fact]
    public void Constructor_final_state_tank_count_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState final = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(
                pre,
                pre,
                final,
                CreateValidRecords(pre, pre, pre)));

        Assert.Equal("finalState", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_index_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                pre.Tanks[0].Movement.Position),
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(1),
                pre.Tanks[1].Movement.Position,
                pre.Tanks[1].Movement.Position),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, pre, pre, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_tank_id_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                pre.Tanks[0].Movement.Position),
            MatchStateTankObstacleCollisionRecord.Unchanged(
                1,
                new TankId(99),
                pre.Tanks[1].Movement.Position,
                pre.Tanks[1].Movement.Position),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, pre, pre, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_initial_position_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                FixedVec2.FromInts(99, 99),
                pre.Tanks[0].Movement.Position),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, pre, pre, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_candidate_position_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(11, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                FixedVec2.FromInts(99, 99)),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, candidate, candidate, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_final_position_mismatch_throws()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(15, 20), FixedVec2.Zero, 100));
        MatchState final = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionRecord[] records =
        {
            MatchStateTankObstacleCollisionRecord.Unchanged(
                0,
                new TankId(0),
                pre.Tanks[0].Movement.Position,
                candidate.Tanks[0].Movement.Position),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankObstacleCollisionResult(pre, candidate, final, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_negative_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            state,
            state,
            state,
            CreateValidRecords(state, state, state));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("index", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_past_end_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankObstacleCollisionResult result = new MatchStateTankObstacleCollisionResult(
            state,
            state,
            state,
            CreateValidRecords(state, state, state));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("index", ex.ParamName);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equalsMethod = typeof(MatchStateTankObstacleCollisionResult).GetMethod(
            nameof(object.Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equalsMethod);
        Assert.Equal(typeof(object), equalsMethod!.DeclaringType);
    }

    [Fact]
    public void Separate_result_instances_with_same_values_are_not_same_reference()
    {
        MatchState pre = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState candidate = pre;
        MatchState final = pre;
        MatchStateTankObstacleCollisionRecord[] records = CreateValidRecords(pre, candidate, final);

        MatchStateTankObstacleCollisionResult first = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            records);
        MatchStateTankObstacleCollisionResult second = new MatchStateTankObstacleCollisionResult(
            pre,
            candidate,
            final,
            records);

        Assert.NotSame(first, second);
    }
}
