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

public sealed class MatchStateTankMovementResultTests
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

    private static MatchStateTankMovementRecord[] CreateValidRecords(MatchState initialState)
    {
        var records = new MatchStateTankMovementRecord[initialState.Tanks.Count];

        for (int i = 0; i < records.Length; i++)
        {
            TankState tank = initialState.Tanks[i];
            records[i] = MatchStateTankMovementRecord.StayedStill(
                i,
                tank.Id,
                tank.Movement);
        }

        return records;
    }

    [Fact]
    public void Constructor_preserves_references()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState final = initial;
        MatchStateTankMovementRecord[] records = CreateValidRecords(initial);

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            initial,
            final,
            records);

        Assert.Same(initial, result.InitialState);
        Assert.Same(final, result.FinalState);
    }

    [Fact]
    public void Constructor_sets_count()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState final = initial;

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            initial,
            final,
            CreateValidRecords(initial));

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Records_are_read_only()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState final = initial;

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            initial,
            final,
            CreateValidRecords(initial));

        Assert.Throws<NotSupportedException>(() =>
            ((IList<MatchStateTankMovementRecord>)result.Records)[0] =
                MatchStateTankMovementRecord.StayedStill(
                    0,
                    new TankId(0),
                    initial.Tanks[0].Movement));
    }

    [Fact]
    public void Constructor_defensively_copies_records()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));
        MatchState final = initial;
        MatchStateTankMovementRecord[] records = CreateValidRecords(initial);

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            initial,
            final,
            records);

        records[0] = MatchStateTankMovementRecord.Moved(
            0,
            new TankId(0),
            initial.Tanks[0].Movement,
            CreateMovement(FixedVec2.FromInts(99, 99), FixedVec2.Zero));

        Assert.Equal(MatchStateTankMovementStatus.StayedStill, result.GetRecordAtIndex(0).Status);
    }

    [Fact]
    public void GetRecordAtIndex_returns_record_in_order()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.FromInts(1, 0), 100));
        MatchState final = initial;

        MovementState tank0Movement = initial.Tanks[0].Movement;
        MovementState tank1Initial = initial.Tanks[1].Movement;
        MovementState tank1Final = CreateMovement(FixedVec2.FromInts(31, 20), FixedVec2.FromInts(1, 0));

        MatchStateTankMovementRecord[] records =
        {
            MatchStateTankMovementRecord.StayedStill(0, new TankId(0), tank0Movement),
            MatchStateTankMovementRecord.Moved(1, new TankId(1), tank1Initial, tank1Final),
        };

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            initial,
            final,
            records);

        Assert.Same(records[0], result.GetRecordAtIndex(0));
        Assert.Same(records[1], result.GetRecordAtIndex(1));
        Assert.True(result.GetRecordAtIndex(1).DidMove);
    }

    [Fact]
    public void Constructor_null_initialState_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankMovementResult(
                null!,
                state,
                CreateValidRecords(state)));

        Assert.Equal("initialState", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_finalState_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankMovementResult(
                state,
                null!,
                CreateValidRecords(state)));

        Assert.Equal("finalState", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_records_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchStateTankMovementResult(state, state, null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_null_record_element_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementRecord[] records = CreateValidRecords(state);
        records[1] = null!;

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementResult(state, state, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_count_mismatch_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementRecord[] records =
        {
            MatchStateTankMovementRecord.StayedStill(0, new TankId(0), state.Tanks[0].Movement),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementResult(state, state, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_final_state_tank_count_mismatch_throws()
    {
        MatchState initial = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));
        MatchState final = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementResult(
                initial,
                final,
                CreateValidRecords(initial)));

        Assert.Equal("finalState", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_index_mismatch_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementRecord[] records =
        {
            MatchStateTankMovementRecord.StayedStill(0, new TankId(0), state.Tanks[0].Movement),
            MatchStateTankMovementRecord.StayedStill(0, new TankId(1), state.Tanks[1].Movement),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementResult(state, state, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_record_tank_id_mismatch_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100),
            CreateTank(1, 1, FixedVec2.FromInts(30, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementRecord[] records =
        {
            MatchStateTankMovementRecord.StayedStill(0, new TankId(0), state.Tanks[0].Movement),
            MatchStateTankMovementRecord.StayedStill(1, new TankId(99), state.Tanks[1].Movement),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchStateTankMovementResult(state, state, records));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_negative_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            state,
            state,
            CreateValidRecords(state));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_past_end_throws()
    {
        MatchState state = CreateState(
            CreateTank(0, 0, FixedVec2.FromInts(10, 20), FixedVec2.Zero, 100));

        MatchStateTankMovementResult result = new MatchStateTankMovementResult(
            state,
            state,
            CreateValidRecords(state));

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Type_does_not_override_object_equals()
    {
        MethodInfo? equalsMethod = typeof(MatchStateTankMovementResult).GetMethod(
            nameof(object.Equals),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: new[] { typeof(object) },
            modifiers: null);

        Assert.NotNull(equalsMethod);
        Assert.Equal(typeof(object), equalsMethod!.DeclaringType);
    }
}
