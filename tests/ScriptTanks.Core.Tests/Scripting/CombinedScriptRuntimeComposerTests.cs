using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Combat;
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

public sealed class CombinedScriptRuntimeComposerTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        Fixed turretRotation = default)
    {
        return CreateTank(
            id,
            ownerSlot,
            position ?? FixedVec2.FromInts(10 + id, 20),
            TankCatalog.BasicTank,
            turretRotation);
    }

    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2 position,
        TankDefinition definition,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            definition,
            new MovementState(position, FixedVec2.Zero),
            definition.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: turretRotation);
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

    private static TankSensorLoadout CreateSensorLoadout(
        SensorState first,
        SensorState? second = null)
    {
        return second.HasValue
            ? new TankSensorLoadout(new[] { first, second.Value })
            : new TankSensorLoadout(new[] { first });
    }

    private static MatchSensorRuntimeState CreateRuntime(
        MatchState state,
        params TankSensorLoadout[] sensorLoadouts)
    {
        return new MatchSensorRuntimeState(
            state,
            new MatchSensorLoadoutState(sensorLoadouts));
    }

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankAimRuntime(
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        Fixed ownerTurretRotation = default,
        SimTick? tick = null)
    {
        MatchState state = CreateMatchState(
            tick ?? new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, ownerPosition, ownerTurretRotation),
            CreateTank(1, 1, enemyPosition));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchSensorRuntimeState CreateTwoTankAimRuntimeWithOwnerDefinition(
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        TankDefinition ownerDefinition,
        Fixed ownerTurretRotation = default)
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, ownerPosition, ownerDefinition, ownerTurretRotation),
            CreateTank(1, 1, enemyPosition));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static TankDefinition CreateStaticTurretTankDefinition()
    {
        return new TankDefinition(
            id: "static_turret_composer_test",
            displayName: "Static Turret Composer Test",
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

    private static Fixed ExpectedGradualAimRotation(
        Fixed initialTurret,
        FixedVec2 owner,
        FixedVec2 target)
    {
        Fixed desired = ExpectedAimRotation(owner, target);
        return FixedRotationTurnStepResolver.ResolveStep(
            initialTurret,
            desired,
            TankCatalog.BasicTank.Stats.TurretTurnRatePerTick).FinalRotation;
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

    private static void AssertFireGeometryFromPostTurretRuntime(
        MatchSensorRuntimeState postTurretRuntime,
        MatchFireRequest fireRequest,
        ProjectileState projectile)
    {
        FireMuzzlePositionResult resolverMuzzle = FireMuzzlePositionResolver.Resolve(
            postTurretRuntime,
            tankIndex: 0,
            new WeaponSlot(0));
        FireVelocityResult resolverVelocity = FireVelocityResolver.Resolve(
            postTurretRuntime,
            tankIndex: 0,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, resolverMuzzle.Status);
        Assert.Equal(FireVelocityStatus.Resolved, resolverVelocity.Status);
        Assert.Equal(resolverMuzzle.MuzzlePosition, fireRequest.MuzzlePosition);
        Assert.Equal(resolverVelocity.FireVelocity, fireRequest.FireVelocity);
        Assert.Equal(new TankId(0), projectile.OwnerTankId);
        Assert.Equal(fireRequest.MuzzlePosition, projectile.Position);
        Assert.Equal(fireRequest.FireVelocity, projectile.VelocityPerTick);
    }

    private static CombinedScriptRuntimeComposerResult RunComposerWithMapping(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mappingResult,
        ProjectileIdSequence projectileIdSequence)
    {
        ScriptMappedSensorRequestApplicationResult sensorApplicationResult =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mappingResult);

        ScriptMappedTurretRequestApplicationResult turretApplicationResult =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, mappingResult);

        ScriptMappedFireRequestConstructionPipelineResult fireConstructionPipelineResult =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                turretApplicationResult.FinalRuntime,
                mappingResult,
                projectileIdSequence);

        ScriptMappedFireRequestApplicationResult fireApplicationResult =
            ScriptMappedFireRequestApplicationPipeline.Apply(
                turretApplicationResult.FinalRuntime,
                fireConstructionPipelineResult);

        ScriptMappedMovementRequestApplicationResult movementApplicationResult =
            ScriptMappedMovementRequestApplicationPipeline.Apply(
                fireApplicationResult.FinalRuntime,
                mappingResult);

        MatchSensorRuntimeState finalRuntime =
            movementApplicationResult.FinalRuntime.WithSensorLoadouts(
                sensorApplicationResult.FinalRuntime.SensorLoadouts);

        return new CombinedScriptRuntimeComposerResult(
            runtime,
            mappingResult.IntegrationResult,
            mappingResult,
            sensorApplicationResult,
            turretApplicationResult,
            fireConstructionPipelineResult,
            fireApplicationResult,
            movementApplicationResult,
            finalRuntime,
            fireConstructionPipelineResult.FinalProjectileIdSequence);
    }

    private static MatchScriptDomainRequestMappingResult CreateAimThenFireMappingForTankZero(
        MatchSensorRuntimeState runtime)
    {
        MatchScriptIntentIntegrationResult aimIntegration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                });

        MatchScriptIntentIntegrationResult fireIntegration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    NoOpWhenAlways(),
                });

        MatchScriptIntentIntegrationRecord aimRecord = aimIntegration.GetRecordAtIndex(0);
        ScriptDomainRequestMappingRecord aimMapping =
            ScriptTranslatedCommandDomainMapper.Map(aimRecord);

        MatchScriptIntentIntegrationRecord fireRecord = fireIntegration.GetRecordAtIndex(0);
        ScriptDomainRequestMappingRecord fireMapping =
            ScriptTranslatedCommandDomainMapper.Map(fireRecord);

        var mappingRecords = new[]
        {
            aimMapping,
            ScriptDomainRequestMappingRecord.Weapon(
                aimRecord.TankIndex,
                aimRecord.TankId,
                fireMapping.TranslationOutput,
                fireRequest: null),
        };

        return new MatchScriptDomainRequestMappingResult(aimIntegration, mappingRecords);
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

    private static ScriptProgram FireWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Fire, argument: string.Empty)),
            });
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

    private static ScriptProgram MoveToPatrolPointWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "patrol",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next")),
            });
    }

    private static ScriptProgram RetreatWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "retreat",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Retreat, "away_from_nearest_visible")),
            });
    }

    private static ScriptCondition Condition(ScriptConditionType type) =>
        new ScriptCondition(type, string.Empty);

    private static ScriptCommand FireCommand() =>
        new ScriptCommand(ScriptCommandType.Fire, string.Empty);

    private static ScriptCommand AimCommand() =>
        new ScriptCommand(ScriptCommandType.AimAtEnemy, "nearest_visible");

    private static ScriptCommand NoOpCommand() =>
        new ScriptCommand(ScriptCommandType.NoOp, string.Empty);

    private static ScriptProgram AimUntilAlignedThenFireProgram()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire-when-aligned",
                    Condition(ScriptConditionType.TurretAligned),
                    FireCommand()),
                new ScriptRoutine(
                    "aim-when-target",
                    Condition(ScriptConditionType.HasAimTarget),
                    AimCommand()),
                new ScriptRoutine(
                    "fallback",
                    ScriptCondition.Always(),
                    NoOpCommand()),
            });
    }

    private static ScriptProgram EnemyVisibleFireElseHasAimTargetProgram()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire-when-visible",
                    Condition(ScriptConditionType.EnemyVisible),
                    FireCommand()),
                new ScriptRoutine(
                    "aim-when-target",
                    Condition(ScriptConditionType.HasAimTarget),
                    AimCommand()),
                new ScriptRoutine(
                    "fallback",
                    ScriptCondition.Always(),
                    NoOpCommand()),
            });
    }

    private static ScriptProgram MissingAimSolutionFallbackProgram()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "fire-aligned",
                    Condition(ScriptConditionType.TurretAligned),
                    FireCommand()),
                new ScriptRoutine(
                    "aim-turning",
                    Condition(ScriptConditionType.TurretTurning),
                    AimCommand()),
                new ScriptRoutine(
                    "noop-missing",
                    Condition(ScriptConditionType.MissingAimSolution),
                    NoOpCommand()),
            });
    }

    private static List<ScriptProgram> AimUntilFireTankZeroPrograms()
    {
        return new List<ScriptProgram>
        {
            AimUntilAlignedThenFireProgram(),
            NoOpWhenAlways(),
        };
    }

    private static MatchScriptIntentIntegrationRecord Tank0IntegrationRecord(
        CombinedScriptRuntimeComposerResult result)
    {
        return result.IntegrationResult.GetRecordAtIndex(0);
    }

    private static FixedVec2 ExpectedPatrolVelocity()
    {
        Fixed maxSpeed = TankCatalog.BasicTank.Stats.MaxVelocityPerTick;
        return FixedVec2.FromInts(1, 0) * maxSpeed;
    }

    private static FixedVec2 ExpectedRetreatVelocity()
    {
        return -ExpectedPatrolVelocity();
    }

    #region Validation

    [Fact]
    public void Run_NullRuntime_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedScriptRuntimeComposer.Run(
                null!,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Run_NullPrograms_ThrowsArgumentNullException()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedScriptRuntimeComposer.Run(
                runtime,
                null!,
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_program_count_mismatch_less_than_tanks()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_program_count_mismatch_greater_than_tanks()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_null_program_element()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram?> { null }!,
                new ProjectileIdSequence(0)));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Result chain

    [Fact]
    public void Run_ReturnsCombinedResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.NotNull(result);
        Assert.Same(runtime, result.InitialRuntime);
        Assert.NotNull(result.IntegrationResult);
        Assert.NotNull(result.MappingResult);
        Assert.NotNull(result.SensorApplicationResult);
        Assert.NotNull(result.TurretApplicationResult);
        Assert.NotNull(result.FireConstructionPipelineResult);
        Assert.NotNull(result.FireApplicationResult);
        Assert.NotNull(result.MovementApplicationResult);
        Assert.NotNull(result.FinalRuntime);
    }

    [Fact]
    public void Run_result_chains_intermediate_references()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Same(runtime, result.InitialRuntime);
        Assert.Same(runtime, result.IntegrationResult.Runtime);
        Assert.Same(result.IntegrationResult, result.MappingResult.IntegrationResult);
        Assert.Same(result.MappingResult, result.SensorApplicationResult.MappingResult);
        Assert.Same(result.MappingResult, result.TurretApplicationResult.MappingResult);
        Assert.Same(
            result.MappingResult,
            result.FireConstructionPipelineResult.ConstructionResult.MappingResult);
        Assert.Same(
            result.FireConstructionPipelineResult,
            result.FireApplicationResult.ConstructionPipelineResult);
        Assert.Same(result.MappingResult, result.MovementApplicationResult.MappingResult);
    }

    [Fact]
    public void Run_result_chains_movement_application_result()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Same(result.MappingResult, result.MovementApplicationResult.MappingResult);
        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
        Assert.Same(
            result.SensorApplicationResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
    }

    [Fact]
    public void Run_result_chains_turret_application_result()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankAimRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20)),
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Same(result.MappingResult, result.TurretApplicationResult.MappingResult);
        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(
            Fixed.Zero,
            result.TurretApplicationResult.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Same(
            result.MappingResult,
            result.FireConstructionPipelineResult.ConstructionResult.MappingResult);
    }

    #endregion

    #region Option A merge

    [Fact]
    public void Run_FinalRuntime_State_ComesFromMovementApplication()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankRuntime(),
                new List<ScriptProgram>
                {
                    MoveToPatrolPointWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
        Assert.NotSame(
            result.FireApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
        Assert.True(result.MovementApplicationResult.Records[0].DidApply);
    }

    [Fact]
    public void Run_FinalRuntime_SensorLoadouts_ComesFromSensorApplication()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankRuntime(),
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Same(
            result.SensorApplicationResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
    }

    [Fact]
    public void Run_DoesNotUseSensorFinalRuntimeForFireConstruction()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.NotSame(runtime, result.SensorApplicationResult.FinalRuntime);
        Assert.Same(
            runtime,
            result.FireConstructionPipelineResult.ConstructionResult.MappingResult.IntegrationResult.Runtime);
    }

    [Fact]
    public void Run_DoesNotReintegratePrograms()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Same(result.IntegrationResult, result.MappingResult.IntegrationResult);
    }

    #endregion

    #region Sensor-only

    [Fact]
    public void Run_sensor_only_scan_applies_sensor_skips_fire()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.True(result.SensorApplicationResult.Records[0].DidApply);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            result.FireApplicationResult.Records[0].Status);
        Assert.Equal(0, result.FinalProjectileIdSequence.NextValue);

        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
        Assert.Same(
            result.SensorApplicationResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Same(runtime.State, result.FinalRuntime.State);
    }

    #endregion

    #region Fire-only

    [Fact]
    public void Run_fire_only_constructs_applies_and_advances_sequence()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(42));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.Equal(
            new ProjectileId(42),
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].FireRequest!.Value.ProjectileId);
        Assert.True(result.FireApplicationResult.Records[0].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.Equal(43, result.FinalProjectileIdSequence.NextValue);
    }

    #endregion

    #region Sensor + Fire

    [Fact]
    public void Run_two_tanks_scan_and_fire_merges_loadouts_and_projectile()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankRuntime(),
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.True(result.SensorApplicationResult.Records[0].DidApply);
        Assert.False(result.SensorApplicationResult.Records[1].DidApply);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].Status);
        Assert.True(result.FireApplicationResult.Records[1].DidApply);

        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.Same(
            result.SensorApplicationResult.FinalRuntime.SensorLoadouts,
            result.FinalRuntime.SensorLoadouts);
        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
    }

    #endregion

    #region Movement

    [Fact]
    public void Run_MovementCommand_AppliesVelocityPerTick()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        FixedVec2 positionBefore = runtime.State.Tanks[0].Movement.Position;

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            result.MovementApplicationResult.Records[0].Status);
        Assert.Equal(ExpectedPatrolVelocity(), result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(positionBefore, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Run_RetreatCommand_AppliesNegativeBodyForwardVelocity()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { RetreatWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.Equal(ExpectedRetreatVelocity(), result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Run_MovementCommand_DoesNotChangePosition()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        FixedVec2 positionBefore = runtime.State.Tanks[0].Movement.Position;

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                new ProjectileIdSequence(0));

        Assert.NotEqual(FixedVec2.Zero, result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(positionBefore, result.FinalRuntime.State.Tanks[0].Movement.Position);
    }

    [Fact]
    public void Run_MovementOnly_SkipsFireAppliesMovement()
    {
        ProjectileIdSequence input = new ProjectileIdSequence(42);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() },
                input);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            result.FireApplicationResult.Records[0].Status);
        Assert.Equal(input, result.FinalProjectileIdSequence);
        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            result.MovementApplicationResult.Records[0].Status);
        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
        Assert.Equal(ExpectedPatrolVelocity(), result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Run_MovementAndFire_PreservesFireStateAndAppliesMovement()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        FixedVec2 tank0PositionBefore = runtime.State.Tanks[0].Movement.Position;

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    MoveToPatrolPointWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            result.MovementApplicationResult.Records[0].Status);
        Assert.Equal(ExpectedPatrolVelocity(), result.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.Equal(tank0PositionBefore, result.FinalRuntime.State.Tanks[0].Movement.Position);

        Assert.True(result.FireApplicationResult.Records[1].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.Equal(1, result.FinalProjectileIdSequence.NextValue);
        Assert.Same(
            result.MovementApplicationResult.FinalRuntime.State,
            result.FinalRuntime.State);
    }

    [Fact]
    public void Run_FinalProjectileIdSequence_ComesFromFireConstruction()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { FireWhenAlways() },
                new ProjectileIdSequence(7));

        Assert.Equal(
            result.FireConstructionPipelineResult.FinalProjectileIdSequence,
            result.FinalProjectileIdSequence);
        Assert.Equal(8, result.FinalProjectileIdSequence.NextValue);
    }

    #endregion

    #region ProjectileIdSequence

    [Fact]
    public void Run_scan_only_leaves_projectile_sequence_unchanged()
    {
        ProjectileIdSequence input = new ProjectileIdSequence(42);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateSingleTankRuntime(),
                new List<ScriptProgram> { ScanWhenAlways() },
                input);

        Assert.Equal(input, result.FinalProjectileIdSequence);
        Assert.Equal(42, result.FinalProjectileIdSequence.NextValue);
    }

    #endregion

    #region Turret

    [Fact]
    public void Run_AimAtEnemy_AppliesTurretRotation()
    {
        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankAimRuntime(OwnerPosition(), FixedVec2.FromInts(20, 20)),
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(Fixed.Zero, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Run_AimAtEnemy_DiagonalEnemy_AppliesGradualTurretRotation()
    {
        FixedVec2 owner = OwnerPosition();
        FixedVec2 enemy = FixedVec2.FromInts(20, 30);
        Fixed initialTurret = Fixed.FromRatio(1, 4);
        Fixed expected = ExpectedGradualAimRotation(initialTurret, owner, enemy);

        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            owner,
            enemy,
            initialTurret);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(expected, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Run_AimAndFire_SeparateTanks()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            OwnerPosition(),
            FixedVec2.FromInts(20, 20),
            Fixed.FromRatio(1, 4));

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    FireWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Fixed expectedOwnerTurret = ExpectedGradualAimRotation(
            Fixed.FromRatio(1, 4),
            OwnerPosition(),
            FixedVec2.FromInts(20, 20));
        Assert.Equal(expectedOwnerTurret, result.FinalRuntime.State.Tanks[0].TurretRotation);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].Status);
        MatchFireRequest fireRequest =
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].FireRequest!.Value;
        Assert.Equal(FixedVec2.FromInts(1, 0), fireRequest.FireVelocity);
        Assert.True(result.FireApplicationResult.Records[1].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);
    }

    #endregion

    #region Full-angle AimAtEnemy composer regression (5.137)

    [Fact]
    public void Run_AimAtEnemy_NorthWestEnemy_AppliesGradualTurretRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(0, 20);
        Fixed expected = ExpectedGradualAimRotation(Fixed.Zero, FullAngleOwner, enemyPosition);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankAimRuntime(FullAngleOwner, enemyPosition),
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(expected, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Run_AimAtEnemy_NonCardinalEnemy_AppliesGradualInverseLookupRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(30, 20);
        Fixed expected = ExpectedGradualAimRotation(Fixed.Zero, FullAngleOwner, enemyPosition);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                CreateTwoTankAimRuntime(FullAngleOwner, enemyPosition),
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(expected, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    [Fact]
    public void Run_AimAtEnemy_TargetAtSamePosition_ReturnsMissingAimSolution()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(FullAngleOwner, FullAngleOwner);
        Fixed ownerTurretBefore = runtime.State.Tanks[0].TurretRotation;

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    AimAtEnemyWhenAlways(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.MissingAimSolution,
            result.TurretApplicationResult.Records[0].Status);
        Assert.False(result.TurretApplicationResult.Records[0].DidApply);
        Assert.Equal(ownerTurretBefore, result.FinalRuntime.State.Tanks[0].TurretRotation);
    }

    #endregion

    #region Full-angle fire/muzzle/velocity regression (5.138)

    private static FixedVec2 ExpectedForward(Fixed rotation)
    {
        FixedRotationDirectionResult result =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.True(result.IsResolved);
        return result.Forward!.Value;
    }

    #endregion

    #region Gradual aim-before-fire composer regression (5.144)

    [Fact]
    public void Run_AimAtEnemyThenFire_DiagonalEnemy_FireUsesSteppedRotation_NotDesiredRotation()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed desiredRotation = ExpectedAimRotation(FullAngleOwner, enemyPosition);
        Fixed steppedRotation =
            ExpectedGradualAimRotation(Fixed.Zero, FullAngleOwner, enemyPosition);
        FixedVec2 expectedForward = ExpectedForward(steppedRotation);
        FixedVec2 expectedVelocity =
            expectedForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick;
        FixedVec2 expectedMuzzle =
            FullAngleOwner + expectedForward * WeaponCatalog.StandardCannon.MuzzleOffsetFromCenter;

        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(FullAngleOwner, enemyPosition);
        MatchScriptDomainRequestMappingResult mapping =
            CreateAimThenFireMappingForTankZero(runtime);

        CombinedScriptRuntimeComposerResult result =
            RunComposerWithMapping(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(124, desiredRotation.Raw);
        Assert.Equal(100, steppedRotation.Raw);
        Assert.NotEqual(desiredRotation, steppedRotation);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.True(result.TurretApplicationResult.Records[0].DidApply);
        Assert.Equal(steppedRotation, result.TurretApplicationResult.Records[0].AppliedTurretRotation);
        Assert.Equal(steppedRotation, result.TurretApplicationResult.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(steppedRotation, result.FinalRuntime.State.Tanks[0].TurretRotation);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].Status);
        Assert.True(result.FireApplicationResult.Records[1].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);

        MatchFireRequest fireRequest =
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].FireRequest!.Value;
        Assert.Equal(new TankId(0), fireRequest.ShooterTankId);
        Assert.Equal(expectedMuzzle, fireRequest.MuzzlePosition);
        Assert.Equal(expectedVelocity, fireRequest.FireVelocity);
        Assert.NotEqual(FixedVec2.FromInts(1, 0), fireRequest.FireVelocity);

        AssertFireGeometryFromPostTurretRuntime(
            result.TurretApplicationResult.FinalRuntime,
            fireRequest,
            result.FinalRuntime.State.Projectiles[0]);
    }

    [Fact]
    public void Run_AimAtEnemyThenFire_WhenTurnStepReachesDesired_FireUsesDesiredRotation()
    {
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 10);
        Fixed initialTurret = Fixed.FromRaw(50);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed steppedRotation =
            ExpectedGradualAimRotation(initialTurret, ownerPosition, enemyPosition);

        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(ownerPosition, enemyPosition, initialTurret);
        MatchScriptDomainRequestMappingResult mapping =
            CreateAimThenFireMappingForTankZero(runtime);

        CombinedScriptRuntimeComposerResult result =
            RunComposerWithMapping(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(Fixed.Zero, desiredRotation);
        Assert.Equal(desiredRotation, steppedRotation);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.True(result.TurretApplicationResult.Records[0].DidApply);
        Assert.Equal(desiredRotation, result.TurretApplicationResult.Records[0].AppliedTurretRotation);
        Assert.Equal(desiredRotation, result.FinalRuntime.State.Tanks[0].TurretRotation);

        MatchFireRequest fireRequest =
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].FireRequest!.Value;
        Assert.Equal(FixedVec2.FromInts(1, 0), fireRequest.FireVelocity);

        AssertFireGeometryFromPostTurretRuntime(
            result.TurretApplicationResult.FinalRuntime,
            fireRequest,
            result.FinalRuntime.State.Projectiles[0]);
    }

    [Fact]
    public void Run_AimAtEnemyThenFire_ZeroTurnRate_FireUsesUnchangedCurrentRotation()
    {
        TankDefinition staticTurret = CreateStaticTurretTankDefinition();
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed initialTurret = Fixed.FromRatio(1, 8);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);

        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntimeWithOwnerDefinition(
            ownerPosition,
            enemyPosition,
            staticTurret,
            initialTurret);
        MatchScriptDomainRequestMappingResult mapping =
            CreateAimThenFireMappingForTankZero(runtime);

        CombinedScriptRuntimeComposerResult result =
            RunComposerWithMapping(runtime, mapping, new ProjectileIdSequence(0));

        Assert.NotEqual(Fixed.Zero, initialTurret);
        Assert.NotEqual(desiredRotation, initialTurret);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.True(result.TurretApplicationResult.Records[0].DidApply);
        Assert.Equal(initialTurret, result.TurretApplicationResult.Records[0].AppliedTurretRotation);
        Assert.Equal(initialTurret, result.FinalRuntime.State.Tanks[0].TurretRotation);

        MatchFireRequest fireRequest =
            result.FireConstructionPipelineResult.ConstructionResult.Records[1].FireRequest!.Value;
        FixedVec2 expectedForward = ExpectedForward(initialTurret);
        FixedVec2 expectedVelocity =
            expectedForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick;
        FixedVec2 expectedMuzzle =
            ownerPosition + expectedForward * WeaponCatalog.StandardCannon.MuzzleOffsetFromCenter;
        FixedVec2 desiredForward = ExpectedForward(desiredRotation);

        Assert.Equal(expectedMuzzle, fireRequest.MuzzlePosition);
        Assert.Equal(expectedVelocity, fireRequest.FireVelocity);
        Assert.NotEqual(desiredForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick, fireRequest.FireVelocity);

        AssertFireGeometryFromPostTurretRuntime(
            result.TurretApplicationResult.FinalRuntime,
            fireRequest,
            result.FinalRuntime.State.Projectiles[0]);
    }

    #endregion

    #region Aim-until-fire condition regression (5.152)

    [Fact]
    public void Run_TurretTurning_SelectsAimAtEnemyInsteadOfFire()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(FullAngleOwner, enemyPosition);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                AimUntilFireTankZeroPrograms(),
                new ProjectileIdSequence(0));

        MatchScriptIntentIntegrationRecord tank0 = Tank0IntegrationRecord(result);
        ScriptRoutineDecision decision = tank0.EvaluationRecord.Result.Decision;

        Assert.Equal(1, decision.RoutineIndex);
        Assert.Equal(ScriptCommandType.AimAtEnemy, decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, tank0.Context.TurretAimStatus.Status);
        Assert.True(tank0.Context.TurretAimStatus.HasTarget);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(100, result.FinalRuntime.State.Tanks[0].TurretRotation.Raw);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.False(result.FireApplicationResult.Records[0].DidApply);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
    }

    [Fact]
    public void Run_TurretAligned_SelectsFireInsteadOfAimAtEnemy()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed alignedRotation = ExpectedAimRotation(FullAngleOwner, enemyPosition);
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(FullAngleOwner, enemyPosition, alignedRotation);

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                AimUntilFireTankZeroPrograms(),
                new ProjectileIdSequence(0));

        MatchScriptIntentIntegrationRecord tank0 = Tank0IntegrationRecord(result);
        ScriptRoutineDecision decision = tank0.EvaluationRecord.Result.Decision;

        Assert.Equal(0, decision.RoutineIndex);
        Assert.Equal(ScriptCommandType.Fire, decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, tank0.Context.TurretAimStatus.Status);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.True(result.FireApplicationResult.Records[0].DidApply);
        Assert.Single(result.FinalRuntime.State.Projectiles);
        Assert.Equal(alignedRotation, result.FinalRuntime.State.Tanks[0].TurretRotation);

        MatchFireRequest fireRequest =
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].FireRequest!.Value;
        AssertFireGeometryFromPostTurretRuntime(
            result.TurretApplicationResult.FinalRuntime,
            fireRequest,
            result.FinalRuntime.State.Projectiles[0]);
    }

    [Fact]
    public void Run_HasAimTarget_WhenEnemyBeyondSensorRange_StillSelectsAimAtEnemy()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            FixedVec2.FromInts(0, 0),
            FixedVec2.FromInts(100, 0));

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    EnemyVisibleFireElseHasAimTargetProgram(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        MatchScriptIntentIntegrationRecord tank0 = Tank0IntegrationRecord(result);
        ScriptRoutineDecision decision = tank0.EvaluationRecord.Result.Decision;

        Assert.False(tank0.Context.EnemyVisible);
        Assert.True(tank0.Context.TurretAimStatus.HasTarget);
        Assert.Equal(1, decision.RoutineIndex);
        Assert.Equal(ScriptCommandType.AimAtEnemy, decision.Command!.Value.Type);

        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.Applied,
            result.TurretApplicationResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.False(result.FireApplicationResult.Records[0].DidApply);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
    }

    [Fact]
    public void Run_MissingAimSolution_SelectsFallbackRoutine()
    {
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(FullAngleOwner, FullAngleOwner);
        Fixed ownerTurretBefore = runtime.State.Tanks[0].TurretRotation;

        CombinedScriptRuntimeComposerResult result =
            CombinedScriptRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    MissingAimSolutionFallbackProgram(),
                    NoOpWhenAlways(),
                },
                new ProjectileIdSequence(0));

        MatchScriptIntentIntegrationRecord tank0 = Tank0IntegrationRecord(result);
        ScriptRoutineDecision decision = tank0.EvaluationRecord.Result.Decision;

        Assert.Equal(ScriptVisibleTurretAimStatus.MissingAimSolution, tank0.Context.TurretAimStatus.Status);
        Assert.Equal(2, decision.RoutineIndex);
        Assert.Equal(ScriptCommandType.NoOp, decision.Command!.Value.Type);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.FireConstructionPipelineResult.ConstructionResult.Records[0].Status);
        Assert.Equal(
            ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret,
            result.TurretApplicationResult.Records[0].Status);
        Assert.False(result.TurretApplicationResult.Records[0].DidApply);
        Assert.Equal(ownerTurretBefore, result.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Empty(result.FinalRuntime.State.Projectiles);
    }

    [Fact]
    public void Step_AimUntilAlignedThenFire_FiresOnlyAfterAlignment()
    {
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed desiredRotation = ExpectedAimRotation(FullAngleOwner, enemyPosition);
        Fixed afterTick1 = ExpectedGradualAimRotation(Fixed.Zero, FullAngleOwner, enemyPosition);
        Fixed afterTick2 = ExpectedGradualAimRotation(afterTick1, FullAngleOwner, enemyPosition);
        List<ScriptProgram> programs = AimUntilFireTankZeroPrograms();
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            FullAngleOwner,
            enemyPosition,
            tick: new SimTick(0));

        CombinedRuntimeTickResult step1 =
            CombinedRuntimeTickPipeline.Step(runtime, programs, new ProjectileIdSequence(0));
        MatchScriptIntentIntegrationRecord tank0Step1 =
            step1.ScriptResult.IntegrationResult.GetRecordAtIndex(0);

        Assert.Equal(1, tank0Step1.EvaluationRecord.Result.Decision.RoutineIndex);
        Assert.Equal(
            ScriptCommandType.AimAtEnemy,
            tank0Step1.EvaluationRecord.Result.Decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, tank0Step1.Context.TurretAimStatus.Status);
        Assert.Equal(afterTick1, step1.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Empty(step1.FinalRuntime.State.Projectiles);

        CombinedRuntimeTickResult step2 =
            CombinedRuntimeTickPipeline.Step(
                step1.FinalRuntime,
                programs,
                step1.FinalProjectileIdSequence);
        MatchScriptIntentIntegrationRecord tank0Step2 =
            step2.ScriptResult.IntegrationResult.GetRecordAtIndex(0);

        Assert.Equal(1, tank0Step2.EvaluationRecord.Result.Decision.RoutineIndex);
        Assert.Equal(
            ScriptCommandType.AimAtEnemy,
            tank0Step2.EvaluationRecord.Result.Decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Turning, tank0Step2.Context.TurretAimStatus.Status);
        Assert.Equal(afterTick2, step2.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(desiredRotation, afterTick2);
        Assert.Empty(step2.FinalRuntime.State.Projectiles);

        CombinedRuntimeTickResult step3 =
            CombinedRuntimeTickPipeline.Step(
                step2.FinalRuntime,
                programs,
                step2.FinalProjectileIdSequence);
        MatchScriptIntentIntegrationRecord tank0Step3 =
            step3.ScriptResult.IntegrationResult.GetRecordAtIndex(0);

        Assert.Equal(0, tank0Step3.EvaluationRecord.Result.Decision.RoutineIndex);
        Assert.Equal(
            ScriptCommandType.Fire,
            tank0Step3.EvaluationRecord.Result.Decision.Command!.Value.Type);
        Assert.Equal(ScriptVisibleTurretAimStatus.Aligned, tank0Step3.Context.TurretAimStatus.Status);
        Assert.Equal(desiredRotation, step3.FinalRuntime.State.Tanks[0].TurretRotation);
        Assert.Single(step3.FinalRuntime.State.Projectiles);

        MatchFireRequest fireRequest =
            step3.ScriptResult.FireConstructionPipelineResult
                .ConstructionResult.Records[0].FireRequest!.Value;
        FixedVec2 expectedForward = ExpectedForward(desiredRotation);
        Assert.Equal(
            expectedForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick,
            fireRequest.FireVelocity);
        Assert.Equal(new TankId(0), step3.FinalRuntime.State.Projectiles[0].OwnerTankId);
    }

    #endregion

    #region Determinism / purity

    [Fact]
    public void Run_deterministic_repeated_calls()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        var programs = new List<ScriptProgram>
        {
            ScanWhenAlways(),
            FireWhenAlways(),
        };
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        CombinedScriptRuntimeComposerResult a =
            CombinedScriptRuntimeComposer.Run(runtime, programs, sequence);
        CombinedScriptRuntimeComposerResult b =
            CombinedScriptRuntimeComposer.Run(runtime, programs, sequence);

        Assert.Equal(a.FinalProjectileIdSequence, b.FinalProjectileIdSequence);
        Assert.Equal(
            a.FinalRuntime.State.Projectiles.Count,
            b.FinalRuntime.State.Projectiles.Count);
        Assert.Equal(
            a.SensorApplicationResult.Records[0].DidApply,
            b.SensorApplicationResult.Records[0].DidApply);
        Assert.Equal(
            a.FireConstructionPipelineResult.ConstructionResult.Records[1].Status,
            b.FireConstructionPipelineResult.ConstructionResult.Records[1].Status);
        Assert.Equal(
            a.FireApplicationResult.Records[1].DidApply,
            b.FireApplicationResult.Records[1].DidApply);
        Assert.Equal(
            a.MovementApplicationResult.Records[1].Status,
            b.MovementApplicationResult.Records[1].Status);
        Assert.Equal(
            a.FinalRuntime.State.Tanks[1].Movement.VelocityPerTick,
            b.FinalRuntime.State.Tanks[1].Movement.VelocityPerTick);
    }

    [Fact]
    public void Run_does_not_mutate_original_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tankBefore = runtime.State.Tanks[0];
        MatchSensorLoadoutState loadoutsBefore = runtime.SensorLoadouts;

        _ = CombinedScriptRuntimeComposer.Run(
            runtime,
            new List<ScriptProgram> { ScanWhenAlways() },
            new ProjectileIdSequence(0));

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tankBefore, runtime.State.Tanks[0]);
        Assert.Same(loadoutsBefore, runtime.SensorLoadouts);
    }

    #endregion
}
