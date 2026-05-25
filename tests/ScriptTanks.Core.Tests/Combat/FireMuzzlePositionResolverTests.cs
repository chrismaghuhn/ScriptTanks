using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Combat;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Combat;

public sealed class FireMuzzlePositionResolverTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateWeaponLoadout(params WeaponState[] weapons)
    {
        return new TankWeaponLoadout(weapons);
    }

    private static MatchState CreateMatchState(
        SimTick tick,
        TankWeaponLoadout[] weaponLoadouts,
        params TankState[] tanks)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            tick,
            tanks,
            weaponLoadouts,
            Array.Empty<ProjectileState>());
    }

    private static TankSensorLoadout CreateSensorLoadout(SensorState first)
    {
        return new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntimeWithTank(
        TankState tank,
        WeaponDefinition weaponDefinition)
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(weaponDefinition)),
            },
            tank);

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        return CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0),
            WeaponCatalog.StandardCannon);
    }

    private static MatchSensorRuntimeState CreateDestroyedSingleTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0).WithCurrentHitPoints(0));

        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    [Fact]
    public void Resolve_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            FireMuzzlePositionResolver.Resolve(null!, tankIndex: 0, new WeaponSlot(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Resolve_NegativeTankIndex_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: -1,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.MuzzlePosition);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolve_TankIndexPastTankCount_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: runtime.State.Tanks.Count,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_DestroyedTank_ReturnsTankDestroyed()
    {
        MatchSensorRuntimeState runtime = CreateDestroyedSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.TankDestroyed, result.Status);
        Assert.Null(result.MuzzlePosition);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolve_MissingWeaponSlot_ReturnsWeaponSlotMissing()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(1));

        Assert.Equal(FireMuzzlePositionStatus.WeaponSlotMissing, result.Status);
        Assert.Null(result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_ValidLiveTankAndWeapon_ReturnsResolved()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.True(result.IsResolved);
        Assert.NotNull(result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_NoLongerReturnsMissingAimDirection_ForValidLiveTankAndSlot()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.NotEqual(FireMuzzlePositionStatus.MissingAimDirection, result.Status);
        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
    }

    [Fact]
    public void Resolve_ZeroTurn_OffsetOne_ReturnsPositionPlusEastOffset()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.Zero),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(11, 20), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_QuarterTurn_OffsetOne_ReturnsPositionPlusNorthOffset()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(1, 4)),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(10, 21), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_HalfTurn_OffsetOne_ReturnsPositionPlusWestOffset()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(1, 2)),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(9, 20), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_ThreeQuarterTurn_OffsetOne_ReturnsPositionPlusSouthOffset()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(3, 4)),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(10, 19), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_RailgunOffsetTwo_ScalesMuzzleDistance()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.Zero),
            WeaponCatalog.Railgun);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(Fixed.FromInt(2), WeaponCatalog.Railgun.MuzzleOffsetFromCenter);
        Assert.Equal(FixedVec2.FromInts(12, 20), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_UsesTankMovementPositionAsAnchor()
    {
        TankState tank = CreateTank(0, 0)
            .WithMovement(new MovementState(FixedVec2.FromInts(5, 7), FixedVec2.Zero))
            .WithTurretRotation(Fixed.Zero);

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            tank,
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(6, 7), result.MuzzlePosition);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Resolve_SameTurretDifferentBody_ReturnsSameMuzzlePosition(int bodyCase)
    {
        Fixed turret = Fixed.FromRatio(1, 4);
        Fixed body = bodyCase switch
        {
            0 => Fixed.Zero,
            1 => Fixed.FromRatio(1, 2),
            2 => Fixed.FromRatio(3, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(bodyCase), bodyCase, null),
        };

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithRotations(body, turret),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(10, 21), result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_MissingWeaponGeometry_not_exercised_by_catalog_weapons_documented()
    {
        // Resolver branch offset <= Fixed.Zero is defensive for future/modded definitions.
        // WeaponDefinition ctor rejects non-positive MuzzleOffsetFromCenter, so catalog
        // weapons cannot reach MissingWeaponGeometry in integration tests without invalid hacks.
        Assert.True(WeaponCatalog.StandardCannon.MuzzleOffsetFromCenter > Fixed.Zero);
        Assert.Equal(Fixed.FromInt(2), WeaponCatalog.Railgun.MuzzleOffsetFromCenter);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(250)]
    [InlineData(500)]
    [InlineData(750)]
    [InlineData(125)]
    [InlineData(1000)]
    [InlineData(-250)]
    public void Resolve_normal_inputs_never_return_MissingAimDirection(long turretRaw)
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRaw(turretRaw)),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.NotEqual(FireMuzzlePositionStatus.MissingAimDirection, result.Status);
    }

    [Fact]
    public void Resolve_DestroyedTankWithInvalidWeaponSlot_ReturnsTankDestroyed_before_WeaponSlotMissing()
    {
        MatchSensorRuntimeState runtime = CreateDestroyedSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(99));

        Assert.Equal(FireMuzzlePositionStatus.TankDestroyed, result.Status);
        Assert.NotEqual(FireMuzzlePositionStatus.WeaponSlotMissing, result.Status);
    }

    [Fact]
    public void Resolve_InvalidTankIndex_ReturnsTankIndexOutOfRange_without_throwing()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 5,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.TankIndexOutOfRange, result.Status);
    }

    [Fact]
    public void Resolve_DoesNotMutateRuntimeOrMatchState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchState stateBefore = runtime.State;
        SimTick tickBefore = stateBefore.CurrentTick;
        int hpBefore = stateBefore.Tanks[0].CurrentHitPoints;

        FireMuzzlePositionResolver.Resolve(runtime, tankIndex: 0, new WeaponSlot(0));

        Assert.Same(stateBefore, runtime.State);
        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(hpBefore, runtime.State.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void Resolve_RepeatedCalls_ReturnEquivalentResults()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireMuzzlePositionResult first = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));
        FireMuzzlePositionResult second = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.MuzzlePosition, second.MuzzlePosition);
    }

    [Theory]
    [InlineData(FireMuzzlePositionStatus.TankIndexOutOfRange)]
    [InlineData(FireMuzzlePositionStatus.TankDestroyed)]
    [InlineData(FireMuzzlePositionStatus.WeaponSlotMissing)]
    public void Resolve_failure_paths_have_null_muzzle_position(FireMuzzlePositionStatus expectedStatus)
    {
        FireMuzzlePositionResult result = expectedStatus switch
        {
            FireMuzzlePositionStatus.TankIndexOutOfRange => FireMuzzlePositionResolver.Resolve(
                CreateSingleTankRuntime(),
                tankIndex: -1,
                new WeaponSlot(0)),
            FireMuzzlePositionStatus.TankDestroyed => FireMuzzlePositionResolver.Resolve(
                CreateDestroyedSingleTankRuntime(),
                tankIndex: 0,
                new WeaponSlot(0)),
            FireMuzzlePositionStatus.WeaponSlotMissing => FireMuzzlePositionResolver.Resolve(
                CreateSingleTankRuntime(),
                tankIndex: 0,
                new WeaponSlot(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedStatus), expectedStatus, null),
        };

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.MuzzlePosition);
        Assert.False(result.IsResolved);
    }

    [Theory]
    [InlineData(FireMuzzlePositionStatus.TankIndexOutOfRange)]
    [InlineData(FireMuzzlePositionStatus.TankDestroyed)]
    [InlineData(FireMuzzlePositionStatus.WeaponSlotMissing)]
    [InlineData(FireMuzzlePositionStatus.MissingAimDirection)]
    [InlineData(FireMuzzlePositionStatus.MissingWeaponGeometry)]
    public void Resolve_NonResolvedStatuses_HaveNullMuzzlePosition(
        FireMuzzlePositionStatus status)
    {
        FireMuzzlePositionResult result = status switch
        {
            FireMuzzlePositionStatus.TankIndexOutOfRange =>
                FireMuzzlePositionResult.TankIndexOutOfRange(),
            FireMuzzlePositionStatus.TankDestroyed =>
                FireMuzzlePositionResult.TankDestroyed(),
            FireMuzzlePositionStatus.WeaponSlotMissing =>
                FireMuzzlePositionResult.WeaponSlotMissing(),
            FireMuzzlePositionStatus.MissingAimDirection =>
                FireMuzzlePositionResult.MissingAimDirection(),
            FireMuzzlePositionStatus.MissingWeaponGeometry =>
                FireMuzzlePositionResult.MissingWeaponGeometry(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

        Assert.Null(result.MuzzlePosition);
        Assert.False(result.IsResolved);
    }

    #region Full-angle muzzle regression (5.138)

    private static Fixed ExpectedAimRotation(FixedVec2 source, FixedVec2 target)
    {
        FixedVec2 delta = target - source;
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    private static FixedVec2 ExpectedForward(Fixed rotation)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.True(result.IsResolved);
        return result.Forward!.Value;
    }

    private static FixedVec2 ExpectedMuzzleFromForward(
        FixedVec2 tankCenter,
        FixedVec2 forward,
        WeaponDefinition weapon)
    {
        return tankCenter + forward * weapon.MuzzleOffsetFromCenter;
    }

    [Fact]
    public void Resolve_DiagonalTurretRotation_UsesFullAngleForwardForMuzzleOffset()
    {
        FixedVec2 center = FixedVec2.FromInts(10, 10);
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed rotation = ExpectedAimRotation(center, target);
        FixedVec2 forward = ExpectedForward(rotation);
        FixedVec2 expectedMuzzle = ExpectedMuzzleFromForward(
            center,
            forward,
            WeaponCatalog.StandardCannon);

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0)
                .WithMovement(new MovementState(center, FixedVec2.Zero))
                .WithTurretRotation(rotation),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(expectedMuzzle, result.MuzzlePosition);
    }

    [Fact]
    public void Resolve_NonCardinalTurretRotation_UsesFullAngleForwardForMuzzleOffset()
    {
        FixedVec2 center = FixedVec2.FromInts(10, 10);
        FixedVec2 target = FixedVec2.FromInts(30, 20);
        Fixed rotation = ExpectedAimRotation(center, target);
        FixedVec2 forward = ExpectedForward(rotation);
        FixedVec2 expectedMuzzle = ExpectedMuzzleFromForward(
            center,
            forward,
            WeaponCatalog.StandardCannon);

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0)
                .WithMovement(new MovementState(center, FixedVec2.Zero))
                .WithTurretRotation(rotation),
            WeaponCatalog.StandardCannon);

        FireMuzzlePositionResult result = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, result.Status);
        Assert.Equal(expectedMuzzle, result.MuzzlePosition);
    }

    #endregion
}
