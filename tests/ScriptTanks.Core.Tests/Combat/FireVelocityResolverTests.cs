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

public sealed class FireVelocityResolverTests
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

    private static WeaponDefinition CreateCustomSpeedWeapon(Fixed speedPerTick)
    {
        return new WeaponDefinition(
            id: "test_custom_speed_cannon",
            displayName: "Test Custom Speed Cannon",
            description: "Weapon definition for fire velocity resolver speed scaling tests.",
            rawDamage: 25,
            cooldownTicks: 30,
            projectileSpeedPerTick: speedPerTick,
            projectileRange: Fixed.FromInt(40),
            projectileRadius: Fixed.FromRatio(1, 4),
            muzzleOffsetFromCenter: Fixed.FromInt(1),
            tags: new[] { "test", "cannon", "velocity" });
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
            FireVelocityResolver.Resolve(null!, tankIndex: 0, new WeaponSlot(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Resolve_NegativeTankIndex_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: -1,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.FireVelocity);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolve_TankIndexPastTankCount_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: runtime.State.Tanks.Count,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.TankIndexOutOfRange, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void Resolve_DestroyedTank_ReturnsTankDestroyed()
    {
        MatchSensorRuntimeState runtime = CreateDestroyedSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.TankDestroyed, result.Status);
        Assert.Null(result.FireVelocity);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolve_MissingWeaponSlot_ReturnsWeaponSlotMissing()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(1));

        Assert.Equal(FireVelocityStatus.WeaponSlotMissing, result.Status);
        Assert.Null(result.FireVelocity);
    }

    [Fact]
    public void Resolve_DestroyedTankWithInvalidWeaponSlot_ReturnsTankDestroyed_before_WeaponSlotMissing()
    {
        MatchSensorRuntimeState runtime = CreateDestroyedSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(99));

        Assert.Equal(FireVelocityStatus.TankDestroyed, result.Status);
        Assert.NotEqual(FireVelocityStatus.WeaponSlotMissing, result.Status);
    }

    [Fact]
    public void Resolve_InvalidTankIndex_ReturnsTankIndexOutOfRange_without_throwing()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 5,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.TankIndexOutOfRange, result.Status);
    }

    [Fact]
    public void Resolve_ValidLiveTank_zero_turn_speed_one_returns_east_velocity()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.Zero),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.True(result.IsResolved);
        Assert.Equal(FixedVec2.FromInts(1, 0), result.FireVelocity);
    }

    [Fact]
    public void Resolve_ValidLiveTank_quarter_turn_returns_north_velocity()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(1, 4)),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(0, 1), result.FireVelocity);
    }

    [Fact]
    public void Resolve_ValidLiveTank_half_turn_returns_west_velocity()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(1, 2)),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(-1, 0), result.FireVelocity);
    }

    [Fact]
    public void Resolve_ValidLiveTank_eighth_turn_returns_resolved_diagonal_velocity()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.FromRatio(1, 8)),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.True(result.IsResolved);
        Assert.NotNull(result.FireVelocity);
        Assert.True(result.FireVelocity.Value.X.Raw > 0);
        Assert.True(result.FireVelocity.Value.Y.Raw > 0);
        Assert.NotEqual(FixedVec2.FromInts(1, 0), result.FireVelocity);
        Assert.NotEqual(FixedVec2.FromInts(0, 1), result.FireVelocity);
        Assert.NotEqual(FixedVec2.FromInts(-1, 0), result.FireVelocity);
        Assert.NotEqual(FixedVec2.FromInts(0, -1), result.FireVelocity);
    }

    [Fact]
    public void Resolve_custom_weapon_speed_scales_velocity()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0).WithTurretRotation(Fixed.Zero),
            CreateCustomSpeedWeapon(Fixed.FromInt(2)));

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(2, 0), result.FireVelocity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Resolve_SameTurretDifferentBody_ReturnsSameFireVelocity(int bodyCase)
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

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(FixedVec2.FromInts(0, 1), result.FireVelocity);
    }

    [Fact]
    public void Resolve_MissingWeaponSpeed_not_exercised_by_catalog_weapons_documented()
    {
        // Resolver branch speed <= Fixed.Zero is defensive for future/modded definitions.
        // WeaponDefinition ctor rejects non-positive ProjectileSpeedPerTick, so catalog
        // weapons cannot reach MissingWeaponSpeed in integration tests without invalid hacks.
        Assert.True(WeaponCatalog.StandardCannon.ProjectileSpeedPerTick > Fixed.Zero);
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

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.NotEqual(FireVelocityStatus.MissingAimDirection, result.Status);
    }

    [Fact]
    public void Resolve_DoesNotMutateRuntimeOrMatchState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchState stateBefore = runtime.State;
        SimTick tickBefore = stateBefore.CurrentTick;
        int hpBefore = stateBefore.Tanks[0].CurrentHitPoints;

        FireVelocityResolver.Resolve(runtime, tankIndex: 0, new WeaponSlot(0));

        Assert.Same(stateBefore, runtime.State);
        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(hpBefore, runtime.State.Tanks[0].CurrentHitPoints);
    }

    [Fact]
    public void Resolve_RepeatedCalls_ReturnEquivalentResults()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        FireVelocityResult first = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));
        FireVelocityResult second = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.FireVelocity, second.FireVelocity);
    }

    [Theory]
    [InlineData(FireVelocityStatus.TankIndexOutOfRange)]
    [InlineData(FireVelocityStatus.TankDestroyed)]
    [InlineData(FireVelocityStatus.WeaponSlotMissing)]
    public void Resolve_failure_paths_have_null_fire_velocity(FireVelocityStatus expectedStatus)
    {
        FireVelocityResult result = expectedStatus switch
        {
            FireVelocityStatus.TankIndexOutOfRange => FireVelocityResolver.Resolve(
                CreateSingleTankRuntime(),
                tankIndex: -1,
                new WeaponSlot(0)),
            FireVelocityStatus.TankDestroyed => FireVelocityResolver.Resolve(
                CreateDestroyedSingleTankRuntime(),
                tankIndex: 0,
                new WeaponSlot(0)),
            FireVelocityStatus.WeaponSlotMissing => FireVelocityResolver.Resolve(
                CreateSingleTankRuntime(),
                tankIndex: 0,
                new WeaponSlot(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedStatus), expectedStatus, null),
        };

        Assert.Equal(expectedStatus, result.Status);
        Assert.Null(result.FireVelocity);
        Assert.False(result.IsResolved);
    }

    #region Full-angle velocity regression (5.138)

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

    private static FixedVec2 ExpectedVelocityFromForward(
        FixedVec2 forward,
        WeaponDefinition weapon)
    {
        return forward * weapon.ProjectileSpeedPerTick;
    }

    [Fact]
    public void Resolve_DiagonalTurretRotation_UsesFullAngleForwardForVelocity()
    {
        FixedVec2 center = FixedVec2.FromInts(10, 10);
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed rotation = ExpectedAimRotation(center, target);
        FixedVec2 forward = ExpectedForward(rotation);
        FixedVec2 expectedVelocity = ExpectedVelocityFromForward(
            forward,
            WeaponCatalog.StandardCannon);

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0)
                .WithMovement(new MovementState(center, FixedVec2.Zero))
                .WithTurretRotation(rotation),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(expectedVelocity, result.FireVelocity);
        Assert.NotEqual(Fixed.Zero, result.FireVelocity!.Value.X);
        Assert.NotEqual(Fixed.Zero, result.FireVelocity!.Value.Y);
    }

    [Fact]
    public void Resolve_NonCardinalTurretRotation_UsesFullAngleForwardForVelocity()
    {
        FixedVec2 center = FixedVec2.FromInts(10, 10);
        FixedVec2 target = FixedVec2.FromInts(30, 20);
        Fixed rotation = ExpectedAimRotation(center, target);
        FixedVec2 forward = ExpectedForward(rotation);
        FixedVec2 expectedVelocity = ExpectedVelocityFromForward(
            forward,
            WeaponCatalog.StandardCannon);

        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithTank(
            CreateTank(0, 0)
                .WithMovement(new MovementState(center, FixedVec2.Zero))
                .WithTurretRotation(rotation),
            WeaponCatalog.StandardCannon);

        FireVelocityResult result = FireVelocityResolver.Resolve(
            runtime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireVelocityStatus.Resolved, result.Status);
        Assert.Equal(expectedVelocity, result.FireVelocity);
        Assert.True(result.FireVelocity!.Value.X > Fixed.Zero);
        Assert.True(result.FireVelocity!.Value.Y > Fixed.Zero);
    }

    #endregion
}
