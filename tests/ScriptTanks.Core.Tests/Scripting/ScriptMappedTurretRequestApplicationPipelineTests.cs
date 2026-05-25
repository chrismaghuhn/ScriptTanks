using System;
using System.Collections.Generic;
using System.Linq;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Scripting;
using ScriptTanks.Core.Sensors;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedTurretRequestApplicationPipelineTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position,
        int hitPoints,
        Fixed bodyRotation = default,
        Fixed turretRotation = default)
    {
        return CreateTank(
            id,
            ownerSlot,
            position,
            hitPoints,
            TankCatalog.BasicTank,
            bodyRotation,
            turretRotation);
    }

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position,
        int hitPoints,
        TankDefinition definition,
        Fixed bodyRotation = default,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            definition,
            new MovementState(position, FixedVec2.Zero),
            hitPoints,
            bodyRotation,
            turretRotation);
    }

    private static TankWeaponLoadout CreateWeaponLoadout()
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });
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

    private static TankSensorLoadout CreateSensorLoadout()
    {
        return new TankSensorLoadout(new[]
        {
            SensorState.Ready(SensorCatalog.BasicRadar),
        });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime(
        FixedVec2? ownerPosition = null,
        Fixed bodyRotation = default,
        Fixed turretRotation = default)
    {
        FixedVec2 position = ownerPosition ?? FixedVec2.FromInts(10, 20);

        MatchState state = CreateMatchState(
            new SimTick(10),
            new[] { CreateWeaponLoadout() },
            CreateTank(0, 0, position, TankCatalog.BasicTank.Stats.MaxHitPoints, bodyRotation, turretRotation));

        return CreateRuntime(state, CreateSensorLoadout());
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime(
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        int enemyHitPoints = int.MaxValue)
    {
        int hp = enemyHitPoints == int.MaxValue
            ? TankCatalog.BasicTank.Stats.MaxHitPoints
            : enemyHitPoints;

        MatchState state = CreateMatchState(
            new SimTick(10),
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            CreateTank(0, 0, ownerPosition, TankCatalog.BasicTank.Stats.MaxHitPoints),
            CreateTank(1, 1, enemyPosition, hp));

        return CreateRuntime(
            state,
            CreateSensorLoadout(),
            CreateSensorLoadout());
    }

    private static MatchSensorRuntimeState CreateThreeTankRuntime(
        FixedVec2 ownerPosition,
        FixedVec2 enemyA,
        FixedVec2 enemyB,
        int enemyAHitPoints = int.MaxValue,
        int enemyBHitPoints = int.MaxValue)
    {
        int hpA = enemyAHitPoints == int.MaxValue
            ? TankCatalog.BasicTank.Stats.MaxHitPoints
            : enemyAHitPoints;
        int hpB = enemyBHitPoints == int.MaxValue
            ? TankCatalog.BasicTank.Stats.MaxHitPoints
            : enemyBHitPoints;

        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(),
                CreateWeaponLoadout(),
                CreateWeaponLoadout(),
            },
            CreateTank(0, 0, ownerPosition, TankCatalog.BasicTank.Stats.MaxHitPoints),
            CreateTank(1, 1, enemyA, hpA),
            CreateTank(2, 2, enemyB, hpB));

        return CreateRuntime(
            state,
            CreateSensorLoadout(),
            CreateSensorLoadout(),
            CreateSensorLoadout());
    }

    private static ScriptProgram AimAtEnemyWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "aim",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.AimAtEnemy, "nearest_visible")),
            });
    }

    private static ScriptProgram ScanWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "scan",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.ScanEnemy, "default")),
            });
    }

    private static ScriptProgram NoOpWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "noop",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.NoOp, string.Empty)),
            });
    }

    private static MatchScriptDomainRequestMappingResult MapPrograms(
        MatchSensorRuntimeState runtime,
        params ScriptProgram[] programs)
    {
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
    }

    private static MatchScriptDomainRequestMappingResult MapAimForTanks(
        MatchSensorRuntimeState runtime,
        params int[] aimTankIndices)
    {
        int tankCount = runtime.State.Tanks.Count;
        var programs = new ScriptProgram[tankCount];

        for (int i = 0; i < tankCount; i++)
        {
            programs[i] = NoOpWhenAlways();
        }

        foreach (int aimIndex in aimTankIndices)
        {
            programs[aimIndex] = AimAtEnemyWhenAlways();
        }

        return MapPrograms(runtime, programs);
    }

    private static MatchScriptDomainRequestMappingResult MapAimForFirstTankOnly(
        MatchSensorRuntimeState runtime)
    {
        return MapAimForTanks(runtime, 0);
    }

    private static ScriptMappedTurretRequestApplicationResult ApplyTurret(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mapping)
    {
        return ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, mapping);
    }

    private static ScriptCommandTranslationOutput TurretTranslationOutput(
        ScriptTranslatedCommandRequestKind kind,
        string payload)
    {
        ScriptCommandType commandType = kind switch
        {
            ScriptTranslatedCommandRequestKind.AimAtEnemy => ScriptCommandType.AimAtEnemy,
            ScriptTranslatedCommandRequestKind.Fire => ScriptCommandType.Fire,
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint =>
                ScriptCommandType.MoveToPatrolPoint,
            _ => ScriptCommandType.ScanEnemy,
        };

        ScriptCommand command = new ScriptCommand(commandType, payload);

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "Turret translation for test.");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(kind, routineIndex: 0, command, payload);

        return new ScriptCommandTranslationOutput(result, request);
    }

    private static MatchScriptDomainRequestMappingResult BuildRejectedInvalidTurretMapping(
        MatchSensorRuntimeState runtime)
    {
        int tankCount = runtime.State.Tanks.Count;
        var programs = new ScriptProgram[tankCount];
        programs[0] = AimAtEnemyWhenAlways();

        for (int i = 1; i < tankCount; i++)
        {
            programs[i] = NoOpWhenAlways();
        }

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        MatchScriptIntentIntegrationRecord record0 = integration.GetRecordAtIndex(0);

        var mappingRecords = new ScriptDomainRequestMappingRecord[tankCount];
        mappingRecords[0] = ScriptDomainRequestMappingRecord.TurretPlaceholder(
            0,
            record0.TankId,
            TurretTranslationOutput(
                ScriptTranslatedCommandRequestKind.Fire,
                string.Empty));

        for (int i = 1; i < tankCount; i++)
        {
            MatchScriptIntentIntegrationRecord record = integration.GetRecordAtIndex(i);
            mappingRecords[i] = ScriptDomainRequestMappingRecord.None(
                i,
                record.TankId,
                record.TranslationOutput);
        }

        return new MatchScriptDomainRequestMappingResult(integration, mappingRecords);
    }

    private static FixedVec2 OwnerPosition() => FixedVec2.FromInts(10, 20);

    private static readonly FixedVec2 FullAngleOwner = FixedVec2.FromInts(10, 10);

    private static Fixed ExpectedAimRotation(FixedVec2 owner, FixedVec2 target)
    {
        FixedVec2 delta = target - owner;
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    private static Fixed ExpectedGradualAimRotation(TankState owner, FixedVec2 targetPosition)
    {
        FixedVec2 delta = targetPosition - owner.Movement.Position;
        FixedRotationAimResolution aim =
            FixedRotationAimResolver.ResolveFromDirection(delta);

        Assert.True(aim.IsResolved);

        return FixedRotationTurnStepResolver.ResolveStep(
            owner.TurretRotation,
            aim.Rotation,
            owner.Definition.Stats.TurretTurnRatePerTick).FinalRotation;
    }

    private static Fixed ExpectedGradualAimRotation(
        FixedVec2 ownerPosition,
        Fixed turretRotation,
        FixedVec2 targetPosition,
        TankDefinition definition)
    {
        FixedVec2 delta = targetPosition - ownerPosition;
        FixedRotationAimResolution aim =
            FixedRotationAimResolver.ResolveFromDirection(delta);

        Assert.True(aim.IsResolved);

        return FixedRotationTurnStepResolver.ResolveStep(
            turretRotation,
            aim.Rotation,
            definition.Stats.TurretTurnRatePerTick).FinalRotation;
    }

    private static void AssertGradualAimApplied(
        ScriptMappedTurretRequestApplicationResult applied,
        int ownerIndex,
        Fixed expectedRotation,
        int enemyIndex,
        Fixed enemyTurretBefore)
    {
        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Equal(expectedRotation, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expectedRotation, applied.FinalRuntime.State.Tanks[ownerIndex].TurretRotation);
        Assert.Equal(
            enemyTurretBefore,
            applied.FinalRuntime.State.Tanks[enemyIndex].TurretRotation);
    }

    private static TankDefinition CreateStaticTurretTankDefinition()
    {
        return new TankDefinition(
            id: "static_turret_test",
            displayName: "Static Turret Test",
            description: "Test tank with zero turret turn rate.",
            stats: new BasicTankStats(
                maxHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints,
                armorReductionPercent: TankCatalog.BasicTank.Stats.ArmorReductionPercent,
                hitboxRadius: TankCatalog.BasicTank.Stats.HitboxRadius,
                maxVelocityPerTick: TankCatalog.BasicTank.Stats.MaxVelocityPerTick,
                bodyTurnRatePerTick: TankCatalog.BasicTank.Stats.BodyTurnRatePerTick,
                turretTurnRatePerTick: Fixed.Zero),
            tags: new[] { "test" });
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntimeWithOwnerDefinition(
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        TankDefinition ownerDefinition,
        Fixed ownerTurretRotation = default)
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            CreateTank(
                0,
                0,
                ownerPosition,
                TankCatalog.BasicTank.Stats.MaxHitPoints,
                ownerDefinition,
                turretRotation: ownerTurretRotation),
            CreateTank(1, 1, enemyPosition, TankCatalog.BasicTank.Stats.MaxHitPoints));

        return CreateRuntime(state, CreateSensorLoadout(), CreateSensorLoadout());
    }

    #region Validation

    [Fact]
    public void Apply_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, AimAtEnemyWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedTurretRequestApplicationPipeline.Apply(null!, mapping));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Apply_NullMappingResult_ThrowsArgumentNullException_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, null!));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Apply_RuntimeDifferentFromIntegrationRuntime_DoesNotThrow()
    {
        MatchSensorRuntimeState integrationRuntime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchScriptDomainRequestMappingResult mapping =
            MapAimForFirstTankOnly(integrationRuntime);

        MatchState postSensorState = integrationRuntime.State.WithCurrentTick(new SimTick(11));
        MatchSensorRuntimeState postSensorRuntime =
            integrationRuntime.WithState(postSensorState);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(postSensorRuntime, mapping);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.NotSame(integrationRuntime, postSensorRuntime);
        Assert.NotSame(
            mapping.IntegrationResult.Runtime,
            applied.FinalRuntime);
    }

    #endregion

    #region Applied

    [Fact]
    public void Apply_AimAtEnemy_EastTarget_SetsTurretRotationZero()
    {
        FixedVec2 owner = OwnerPosition();
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(owner, FixedVec2.FromInts(20, 20));
        Fixed turretBefore = runtime.State.Tanks[0].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Equal(Fixed.Zero, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(Fixed.Zero, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(turretBefore, runtime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_NorthTarget_ClampsTowardQuarterRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(10, 30);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            Fixed.Zero,
            target,
            TankCatalog.BasicTank);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(Fixed.FromRaw(100), expected);
    }

    [Fact]
    public void Apply_AimAtEnemy_WestTarget_ClampsTowardHalfRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(0, 20);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            Fixed.Zero,
            target,
            TankCatalog.BasicTank);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(Fixed.FromRaw(100), expected);
    }

    [Fact]
    public void Apply_AimAtEnemy_SouthTarget_ClampsTowardThreeQuarterRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(10, 10);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            Fixed.Zero,
            target,
            TankCatalog.BasicTank);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(Fixed.FromRaw(900), expected);
    }

    #endregion

    #region Skipped and failures

    [Fact]
    public void Apply_NonTurretRecord_ReturnsSkippedNotTurret()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapPrograms(runtime, ScanWhenAlways()));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret,
            applied.Records[0].Status);
        Assert.False(applied.Records[0].DidApply);
        Assert.Same(runtime.State, applied.FinalRuntime.State);
    }

    [Fact]
    public void Apply_NonTurretRecord_DoesNotChangeState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        Fixed turretBefore = runtime.State.Tanks[0].TurretRotation;

        ApplyTurret(runtime, MapPrograms(runtime, ScanWhenAlways()));

        Assert.Equal(turretBefore, runtime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_NoAliveEnemy_ReturnsNoTarget()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.NoTarget,
            applied.Records[0].Status);
        Assert.False(applied.Records[0].DidApply);
        Assert.Null(applied.Records[0].AppliedTurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_DiagonalEnemy_AppliesTurnRateLimitedRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(20, 30);
        Fixed desired = ExpectedAimRotation(owner, target);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            Fixed.Zero,
            target,
            TankCatalog.BasicTank);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.NotEqual(desired, expected);
    }

    [Fact]
    public void Apply_TankIndexOutOfRange_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { AimAtEnemyWhenAlways() });

        MatchScriptIntentIntegrationRecord record = integration.GetRecordAtIndex(0);

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.TurretPlaceholder(
                tankIndex: 99,
                record.TankId,
                record.TranslationOutput),
        };

        MatchScriptDomainRequestMappingResult mapping =
            new MatchScriptDomainRequestMappingResult(integration, mappingRecords);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange,
            applied.Records[0].Status);
    }

    [Fact]
    public void Apply_DestroyedTank_ReturnsTankDestroyed()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        TankState destroyed = runtime.State.Tanks[0].WithCurrentHitPoints(0);
        MatchState state = runtime.State.WithTanks(new[]
        {
            destroyed,
            runtime.State.Tanks[1],
        });
        MatchSensorRuntimeState destroyedRuntime = runtime.WithState(state);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(
                destroyedRuntime,
                MapAimForFirstTankOnly(destroyedRuntime));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.TankDestroyed,
            applied.Records[0].Status);
        Assert.Equal(
            destroyed.TurretRotation,
            applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_UnsupportedTurretRequestKind_ReturnsRejectedInvalidTurretRequest()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchScriptDomainRequestMappingResult mapping =
            BuildRejectedInvalidTurretMapping(runtime);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest,
            applied.Records[0].Status);
        Assert.Same(runtime.State, applied.FinalRuntime.State);
    }

    #endregion

    #region Nearest enemy

    [Fact]
    public void Apply_AimAtEnemy_SelectsNearestAliveEnemy()
    {
        FixedVec2 owner = OwnerPosition();
        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            owner,
            enemyA: FixedVec2.FromInts(100, 20),
            enemyB: FixedVec2.FromInts(20, 20));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.Zero, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_TieBreaksByLowerTankIndex()
    {
        FixedVec2 owner = OwnerPosition();
        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            owner,
            enemyA: FixedVec2.FromInts(20, 20),
            enemyB: FixedVec2.FromInts(0, 20));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.Zero, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_IgnoresDestroyedEnemy()
    {
        FixedVec2 owner = OwnerPosition();
        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            owner,
            enemyA: FixedVec2.FromInts(20, 20),
            enemyB: FixedVec2.FromInts(100, 20),
            enemyAHitPoints: 0,
            enemyBHitPoints: TankCatalog.BasicTank.Stats.MaxHitPoints);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.Zero, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    #endregion

    #region Full-angle AimAtEnemy regression (5.136)

    [Fact]
    public void Apply_AimAtEnemy_NorthWestEnemy_AppliesGradualTurretRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(0, 20);

        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(FullAngleOwner, enemyPosition);
        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], enemyPosition);
        const int enemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[enemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            enemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_SouthWestEnemy_AppliesGradualTurretRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(0, 0);

        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(FullAngleOwner, enemyPosition);
        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], enemyPosition);
        const int enemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[enemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            enemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_SouthEastEnemy_AppliesGradualTurretRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 0);

        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(FullAngleOwner, enemyPosition);
        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], enemyPosition);
        const int enemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[enemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            enemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_NonCardinalEnemy_AppliesGradualInverseLookupRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(30, 20);

        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(FullAngleOwner, enemyPosition);
        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], enemyPosition);
        const int enemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[enemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            enemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_ChoosesNearestAliveEnemy_AndAppliesGradualRotation()
    {
        FixedVec2 nearEnemy = FixedVec2.FromInts(20, 20);

        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            FullAngleOwner,
            enemyA: nearEnemy,
            enemyB: FixedVec2.FromInts(50, 10));

        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], nearEnemy);
        const int winningEnemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[winningEnemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            winningEnemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_SkipsDestroyedNearestEnemy_AndAppliesGradualRotationToAliveTarget()
    {
        FixedVec2 aliveEnemy = FixedVec2.FromInts(0, 20);

        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            FullAngleOwner,
            enemyA: FixedVec2.FromInts(20, 20),
            enemyB: aliveEnemy,
            enemyAHitPoints: 0);

        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], aliveEnemy);
        const int winningEnemyIndex = 2;
        Fixed enemyTurretBefore = runtime.State.Tanks[winningEnemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            winningEnemyIndex,
            enemyTurretBefore);
    }

    [Fact]
    public void Apply_AimAtEnemy_EqualDistanceTargets_KeepsExistingTieBreakWithGradualRotation()
    {
        FixedVec2 winningEnemy = FixedVec2.FromInts(20, 20);
        FixedVec2 losingEnemy = FixedVec2.FromInts(20, 0);

        MatchSensorRuntimeState runtime = CreateThreeTankRuntime(
            FullAngleOwner,
            enemyA: winningEnemy,
            enemyB: losingEnemy);

        Fixed expected = ExpectedGradualAimRotation(runtime.State.Tanks[0], winningEnemy);
        Fixed losingRotation = ExpectedGradualAimRotation(runtime.State.Tanks[0], losingEnemy);
        const int winningEnemyIndex = 1;
        Fixed enemyTurretBefore = runtime.State.Tanks[winningEnemyIndex].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        AssertGradualAimApplied(
            applied,
            ownerIndex: 0,
            expected,
            winningEnemyIndex,
            enemyTurretBefore);
        Assert.NotEqual(losingRotation, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_TargetAtSamePosition_ReturnsMissingAimSolution()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(FullAngleOwner, FullAngleOwner);
        Fixed ownerTurretBefore = runtime.State.Tanks[0].TurretRotation;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.MissingAimSolution,
            applied.Records[0].Status);
        Assert.False(applied.Records[0].DidApply);
        Assert.Null(applied.Records[0].AppliedTurretRotation);
        Assert.Equal(ownerTurretBefore, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(ownerTurretBefore, runtime.State.Tanks[0].TurretRotation);
    }

    #endregion

    #region Gradual turret rotation integration (5.143)

    [Fact]
    public void Apply_AimAtEnemy_DiagonalEnemy_AppliesTurnRateLimitedRotationAtFullAnglePositions()
    {
        FixedVec2 owner = FullAngleOwner;
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed desired = ExpectedAimRotation(owner, target);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            Fixed.Zero,
            target,
            TankCatalog.BasicTank);

        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);
        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(Fixed.FromRaw(124), desired);
        Assert.Equal(Fixed.FromRaw(100), expected);
        Assert.NotEqual(desired, expected);
    }

    [Fact]
    public void Apply_AimAtEnemy_WhenDesiredWithinTurnRate_SnapsToDesiredRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed turret = Fixed.FromRaw(50);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            turret,
            target,
            TankCatalog.BasicTank);

        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);
        TankState[] tanks = runtime.State.Tanks.ToArray();
        tanks[0] = tanks[0].WithTurretRotation(turret);
        runtime = runtime.WithState(runtime.State.WithTanks(tanks));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.Zero, expected);
        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_CardinalTargetBeyondTurnRate_ClampsTowardCardinalRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed turret = Fixed.FromRatio(1, 4);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            turret,
            target,
            TankCatalog.BasicTank);

        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);
        TankState[] tanks = runtime.State.Tanks.ToArray();
        tanks[0] = tanks[0].WithTurretRotation(turret);
        runtime = runtime.WithState(runtime.State.WithTanks(tanks));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.FromRaw(150), expected);
        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_CardinalTargetWithinTurnRate_ReachesCardinalRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed turret = Fixed.FromRaw(50);
        Fixed expected = ExpectedGradualAimRotation(
            owner,
            turret,
            target,
            TankCatalog.BasicTank);

        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(owner, target);
        TankState[] tanks = runtime.State.Tanks.ToArray();
        tanks[0] = tanks[0].WithTurretRotation(turret);
        runtime = runtime.WithState(runtime.State.WithTanks(tanks));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(Fixed.Zero, expected);
        Assert.Equal(expected, applied.Records[0].AppliedTurretRotation);
    }

    [Fact]
    public void Apply_AimAtEnemy_ZeroTurnRate_DoesNotMoveButReturnsApplied()
    {
        TankDefinition staticTurret = CreateStaticTurretTankDefinition();
        FixedVec2 owner = FullAngleOwner;
        FixedVec2 target = FixedVec2.FromInts(20, 20);
        Fixed initialTurret = Fixed.FromRatio(1, 8);

        MatchSensorRuntimeState runtime = CreateTwoTankRuntimeWithOwnerDefinition(
            owner,
            target,
            staticTurret,
            initialTurret);

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Equal(initialTurret, applied.Records[0].AppliedTurretRotation);
        Assert.Equal(initialTurret, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    #endregion

    #region State preservation

    [Fact]
    public void Apply_AppliedTurret_DoesNotChangeBodyRotation()
    {
        Fixed bodyRotation = Fixed.FromRatio(1, 4);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            OwnerPosition(),
            FixedVec2.FromInts(20, 20));
        runtime = runtime.WithState(
            runtime.State.WithTanks(new[]
            {
                runtime.State.Tanks[0].WithBodyRotation(bodyRotation),
                runtime.State.Tanks[1],
            }));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(bodyRotation, applied.FinalRuntime.State.Tanks[0].BodyRotation);
    }

    [Fact]
    public void Apply_AppliedTurret_DoesNotChangeMovement()
    {
        FixedVec2 position = OwnerPosition();
        FixedVec2 velocity = FixedVec2.FromInts(3, 4);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            position,
            FixedVec2.FromInts(20, 20));
        TankState tank = runtime.State.Tanks[0];
        TankState updated = tank.WithMovement(tank.Movement.WithVelocityPerTick(velocity));
        runtime = runtime.WithState(
            runtime.State.WithTanks(new[] { updated, runtime.State.Tanks[1] }));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(position, applied.FinalRuntime.State.Tanks[0].Movement.Position);
        Assert.Equal(velocity, applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_AppliedTurret_DoesNotChangeHitPointsProjectilesOrTick()
    {
        SimTick tick = new SimTick(42);
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            OwnerPosition(),
            FixedVec2.FromInts(20, 20));
        runtime = runtime.WithState(runtime.State.WithCurrentTick(tick));

        int hpBefore = runtime.State.Tanks[0].CurrentHitPoints;
        int projectileCount = runtime.State.Projectiles.Count;

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Equal(hpBefore, applied.FinalRuntime.State.Tanks[0].CurrentHitPoints);
        Assert.Equal(tick, applied.FinalRuntime.State.CurrentTick);
        Assert.Equal(projectileCount, applied.FinalRuntime.State.Projectiles.Count);
    }

    [Fact]
    public void Apply_PreservesSensorLoadoutsReference()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Same(runtime.SensorLoadouts, applied.FinalRuntime.SensorLoadouts);
    }

    #endregion

    #region Ordering and threading

    [Fact]
    public void Apply_MultipleTurretRecords_AppliesInRecordOrder()
    {
        FixedVec2 owner = OwnerPosition();
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            owner,
            FixedVec2.FromInts(20, 20));

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { AimAtEnemyWhenAlways(), AimAtEnemyWhenAlways() });

        MatchScriptIntentIntegrationRecord record0 = integration.GetRecordAtIndex(0);
        MatchScriptIntentIntegrationRecord record1 = integration.GetRecordAtIndex(1);

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.TurretPlaceholder(
                0,
                record0.TankId,
                record0.TranslationOutput),
            ScriptDomainRequestMappingRecord.TurretPlaceholder(
                0,
                record1.TankId,
                record1.TranslationOutput),
        };

        MatchScriptDomainRequestMappingResult mapping =
            new MatchScriptDomainRequestMappingResult(integration, mappingRecords);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            applied.Records[1].Status);
        Assert.Equal(Fixed.Zero, applied.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_TwoTanks_BothApplied()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(10, 0));

        ScriptMappedTurretRequestApplicationResult applied =
            ApplyTurret(runtime, MapAimForTanks(runtime, 0, 1));

        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.Equal(ScriptMappedTurretRequestApplicationStatus.Applied, applied.Records[1].Status);

        TankState owner0 = runtime.State.Tanks[0];
        TankState owner1 = runtime.State.Tanks[1];
        Fixed expected0 = ExpectedGradualAimRotation(owner0, owner1.Movement.Position);
        Fixed expected1 = ExpectedGradualAimRotation(owner1, owner0.Movement.Position);

        Assert.Equal(expected0, applied.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(expected1, applied.FinalRuntime.State.Tanks[1].TurretRotation);
    }

    [Fact]
    public void Apply_ResultCountMatchesMappingCount()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchScriptDomainRequestMappingResult mapping =
            MapAimForFirstTankOnly(runtime);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Equal(mapping.Count, applied.Count);
    }

    [Fact]
    public void Apply_ResultRecordsPreserveOrder()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(10, 0));

        MatchScriptDomainRequestMappingResult mapping =
            MapAimForTanks(runtime, 0, 1);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Same(applied.Records[0], applied.GetRecordAtIndex(0));
        Assert.Same(applied.Records[1], applied.GetRecordAtIndex(1));
        Assert.Equal(0, applied.GetRecordAtIndex(0).RecordIndex);
        Assert.Equal(1, applied.GetRecordAtIndex(1).RecordIndex);
    }

    [Fact]
    public void Apply_DoesNotMutateInputRuntime()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchState stateBefore = runtime.State;
        Fixed turretBefore = runtime.State.Tanks[0].TurretRotation;

        ApplyTurret(runtime, MapAimForFirstTankOnly(runtime));

        Assert.Same(stateBefore, runtime.State);
        Assert.Equal(turretBefore, runtime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_RepeatedCalls_ReturnEquivalentResults()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchScriptDomainRequestMappingResult mapping =
            MapAimForFirstTankOnly(runtime);

        ScriptMappedTurretRequestApplicationResult a = ApplyTurret(runtime, mapping);
        ScriptMappedTurretRequestApplicationResult b = ApplyTurret(runtime, mapping);

        Assert.Equal(a.Records[0].Status, b.Records[0].Status);
        Assert.Equal(a.Records[0].AppliedTurretRotation, b.Records[0].AppliedTurretRotation);
        Assert.Equal(
            a.FinalRuntime.State.Tanks[0].TurretRotation,
            b.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Apply_PreservesMappingResultReference()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20));
        MatchScriptDomainRequestMappingResult mapping =
            MapAimForFirstTankOnly(runtime);

        ScriptMappedTurretRequestApplicationResult applied = ApplyTurret(runtime, mapping);

        Assert.Same(mapping, applied.MappingResult);
    }

    #endregion
}
