using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptVisibleTurretAimStatusResolverTests
{
    private static FixedVec2 Vec(int x, int y) => FixedVec2.FromInts(x, y);

    private static Fixed R(int raw) => Fixed.FromRaw(raw);

    private static TankId Tank(int value) => new(value);

    private static TankState CreateTank(
        int id,
        FixedVec2 position,
        int hitPoints,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(id),
            TankCatalog.BasicTank,
            new MovementState(position, FixedVec2.Zero),
            hitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation);
    }

    private static TankWeaponLoadout CreateWeaponLoadout()
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });
    }

    private static MatchState CreateState(params TankState[] tanks)
    {
        var loadouts = new TankWeaponLoadout[tanks.Length];
        for (int i = 0; i < loadouts.Length; i++)
        {
            loadouts[i] = CreateWeaponLoadout();
        }

        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(10),
            tanks,
            loadouts,
            Array.Empty<ProjectileState>());
    }

    private static Fixed ExpectedAimRotation(FixedVec2 source, FixedVec2 target)
    {
        FixedVec2 delta = target - source;
        FixedRotationAimResolution resolution =
            FixedRotationAimResolver.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    [Fact]
    public void Resolve_NullState_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptVisibleTurretAimStatusResolver.Resolve(null!, 0));

        Assert.Equal("state", ex.ParamName);
    }

    [Fact]
    public void Resolve_NegativeTankIndex_Throws()
    {
        MatchState state = CreateState(CreateTank(0, Vec(0, 0), 100));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptVisibleTurretAimStatusResolver.Resolve(state, -1));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Resolve_TankIndexOutOfRange_Throws()
    {
        MatchState state = CreateState(CreateTank(0, Vec(0, 0), 100));

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 1));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void Resolve_OwnerDestroyed_ReturnsOwnerDestroyed()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 20), hitPoints: 0),
            CreateTank(1, Vec(20, 20), hitPoints: 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, tankIndex: 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.OwnerDestroyed, result.Status);
        Assert.Equal(Tank(0), result.TankId);
        Assert.Equal(0, result.TankIndex);
        Assert.False(result.HasTarget);
        Assert.Null(result.TargetTankId);
        Assert.Null(result.TargetTankIndex);
        Assert.Null(result.DesiredRotation);
        Assert.Null(result.AlignmentDelta);
        Assert.Null(result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Resolve_NoOtherTanks_ReturnsNoTarget()
    {
        MatchState state = CreateState(CreateTank(0, Vec(0, 0), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.NoTarget, result.Status);
        Assert.False(result.HasTarget);
        Assert.Null(result.DesiredRotation);
    }

    [Fact]
    public void Resolve_AllOtherTanksDestroyed_ReturnsNoTarget()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(0, 0), 100),
            CreateTank(1, Vec(10, 0), hitPoints: 0));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.NoTarget, result.Status);
        Assert.False(result.HasTarget);
    }

    [Fact]
    public void Resolve_TargetAtSamePosition_ReturnsMissingAimSolution()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100),
            CreateTank(1, Vec(10, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.MissingAimSolution, result.Status);
        Assert.Equal(Tank(1), result.TargetTankId);
        Assert.Equal(1, result.TargetTankIndex);
        Assert.Null(result.DesiredRotation);
        Assert.Null(result.AlignmentDelta);
        Assert.Null(result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Resolve_TargetEastAndCurrentEast_ReturnsAligned()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: Fixed.Zero),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, result.Status);
        Assert.True(result.IsAligned);
        Assert.Equal(Fixed.Zero, result.DesiredRotation);
        Assert.Equal(Fixed.Zero, result.AlignmentDelta);
        Assert.Equal(Fixed.Zero, result.AlignmentErrorMagnitude);
    }

    [Fact]
    public void Resolve_CurrentRotationNormalizesToDesired_ReturnsAligned()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: R(1000)),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, result.Status);
        Assert.Equal(Fixed.Zero, result.CurrentRotation);
        Assert.True(result.IsAligned);
    }

    [Fact]
    public void Resolve_TargetEastAndCurrentNorth_ReturnsTurningWithNegativeDelta()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: R(250)),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(R(-250), result.AlignmentDelta);
        Assert.Equal(R(250), result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Resolve_TargetNorthAndCurrentEast_ReturnsTurningWithPositiveDelta()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: Fixed.Zero),
            CreateTank(1, Vec(10, 20), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(R(250), result.AlignmentDelta);
        Assert.Equal(R(250), result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Resolve_ExactHalfTurnTie_UsesCounterClockwiseDelta()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: Fixed.Zero),
            CreateTank(1, Vec(0, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(R(500), result.AlignmentDelta);
        Assert.Equal(R(500), result.AlignmentErrorMagnitude);
    }

    [Fact]
    public void Resolve_FullAngleDiagonalTarget_ReturnsTurningWithDesiredFromAimResolver()
    {
        FixedVec2 owner = Vec(10, 10);
        FixedVec2 enemy = Vec(20, 20);
        Fixed expectedDesired = ExpectedAimRotation(owner, enemy);

        MatchState state = CreateState(
            CreateTank(0, owner, 100, turretRotation: Fixed.Zero),
            CreateTank(1, enemy, 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(expectedDesired, result.DesiredRotation);
        Assert.Equal(R(124), result.AlignmentDelta);
        Assert.Equal(R(124), result.AlignmentErrorMagnitude);
        Assert.False(result.IsAligned);
    }

    [Fact]
    public void Resolve_SelectsNearestAliveEnemyByPosition()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(0, 0), 100),
            CreateTank(1, Vec(5, 5), 100),
            CreateTank(2, Vec(20, 0), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(1, result.TargetTankIndex);
        Assert.Equal(Tank(1), result.TargetTankId);
    }

    [Fact]
    public void Resolve_SkipsDestroyedNearestEnemy()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100),
            CreateTank(1, Vec(20, 20), hitPoints: 0),
            CreateTank(2, Vec(0, 20), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(2, result.TargetTankIndex);
        Assert.Equal(Tank(2), result.TargetTankId);
    }

    [Fact]
    public void Resolve_EqualDistanceTargets_UsesLowerTankIndex()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100),
            CreateTank(1, Vec(20, 20), 100),
            CreateTank(2, Vec(20, 0), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(1, result.TargetTankIndex);
    }

    [Fact]
    public void Resolve_DoesNotUseSensorVisibilityContext()
    {
        // Position-based nearest alive enemy: target is selected without ScriptEvaluationContext
        // or sensor-range visibility (resolver has no context parameter).
        MatchState state = CreateState(
            CreateTank(0, Vec(0, 0), 100),
            CreateTank(1, Vec(100, 100), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(Tank(1), result.TargetTankId);
        Assert.NotEqual(ScriptVisibleTurretAimStatus.NoTarget, result.Status);
    }

    [Fact]
    public void Resolve_ResultPreservesOwnerAndTargetIdsAndIndexes()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: R(250)),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(Tank(0), result.TankId);
        Assert.Equal(0, result.TankIndex);
        Assert.Equal(Tank(1), result.TargetTankId);
        Assert.Equal(1, result.TargetTankIndex);
    }

    [Fact]
    public void Resolve_AlignedResultPreservesNormalizedCurrentRotation()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: R(1000)),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, result.Status);
        Assert.Equal(Fixed.Zero, result.CurrentRotation);
        Assert.NotEqual(R(1000), result.CurrentRotation);
    }

    [Fact]
    public void Resolve_TurningResultHasTargetAndDesiredFields()
    {
        MatchState state = CreateState(
            CreateTank(0, Vec(10, 10), 100, turretRotation: R(250)),
            CreateTank(1, Vec(20, 10), 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.True(result.HasTarget);
        Assert.NotNull(result.DesiredRotation);
        Assert.NotNull(result.AlignmentDelta);
        Assert.NotNull(result.AlignmentErrorMagnitude);
    }

    [Fact]
    public void Resolve_TurningWrapAroundNegativeDelta_UsesShortestPath()
    {
        FixedVec2 ownerPos = Vec(10, 10);
        Fixed turret = R(50);
        FixedVec2 enemyPos = ownerPos + ForwardDirection(R(950));
        Fixed desired = ExpectedAimRotation(ownerPos, enemyPos);

        FixedRotationTurnStepResult expectedStep = FixedRotationTurnStepResolver.ResolveStep(
            turret,
            desired,
            Fixed.One);

        MatchState state = CreateState(
            CreateTank(0, ownerPos, 100, turret),
            CreateTank(1, enemyPos, 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(expectedStep.AppliedTurnDelta, result.AlignmentDelta);
        Assert.Equal(expectedStep.AppliedTurnMagnitude, result.AlignmentErrorMagnitude);
        Assert.True(result.AlignmentDelta < Fixed.Zero);
    }

    [Fact]
    public void Resolve_TurningWrapAroundPositiveDelta_UsesShortestPath()
    {
        FixedVec2 ownerPos = Vec(10, 10);
        Fixed turret = R(950);
        FixedVec2 enemyPos = ownerPos + ForwardDirection(R(50));
        Fixed desired = ExpectedAimRotation(ownerPos, enemyPos);

        FixedRotationTurnStepResult expectedStep = FixedRotationTurnStepResolver.ResolveStep(
            turret,
            desired,
            Fixed.One);

        MatchState state = CreateState(
            CreateTank(0, ownerPos, 100, turret),
            CreateTank(1, enemyPos, 100));

        ScriptVisibleTurretAimStatusResult result =
            ScriptVisibleTurretAimStatusResolver.Resolve(state, 0);

        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, result.Status);
        Assert.Equal(expectedStep.AppliedTurnDelta, result.AlignmentDelta);
        Assert.Equal(expectedStep.AppliedTurnMagnitude, result.AlignmentErrorMagnitude);
        Assert.True(result.AlignmentDelta > Fixed.Zero);
    }

    private static FixedVec2 ForwardDirection(Fixed rotation)
    {
        FixedRotationDirectionResult forward =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.True(forward.IsResolved);
        return forward.Forward!.Value;
    }
}
