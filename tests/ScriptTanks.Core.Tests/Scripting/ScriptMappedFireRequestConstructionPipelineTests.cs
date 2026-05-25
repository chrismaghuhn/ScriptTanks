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

public sealed class ScriptMappedFireRequestConstructionPipelineTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
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

    private static ScriptProgram AimWhenAlways()
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

    private static ScriptProgram RetreatWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "retreat",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.Retreat, string.Empty)),
            });
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

    private static MatchSensorRuntimeState CreateFourTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1),
            CreateTank(2, 2),
            CreateTank(3, 3));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchScriptDomainRequestMappingResult MapPrograms(
        MatchSensorRuntimeState runtime,
        params ScriptProgram[] programs)
    {
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
    }

    private static MatchScriptDomainRequestMappingResult MapAimForFirstTankOnly(
        MatchSensorRuntimeState runtime)
    {
        return MapPrograms(runtime, AimWhenAlways(), NoOpWhenAlways());
    }

    private static MatchSensorRuntimeState CreateTwoTankAimRuntime()
    {
        return CreateTwoTankAimRuntime(
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(20, 20),
            Fixed.FromRatio(1, 4));
    }

    private static readonly FixedVec2 FullAngleOwner = FixedVec2.FromInts(10, 10);

    private static MatchSensorRuntimeState CreateTwoTankAimRuntime(
        FixedVec2 ownerPosition,
        FixedVec2 enemyPosition,
        Fixed ownerTurretRotation = default)
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
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

    private static Fixed ExpectedAimRotation(FixedVec2 owner, FixedVec2 target)
    {
        FixedVec2 delta = target - owner;
        FixedRotationAimResolution resolution =
            FixedRotationInverseLookup.ResolveFromDirection(delta);

        Assert.True(resolution.IsResolved);
        return resolution.Rotation;
    }

    private static Fixed ExpectedTurnStep(Fixed current, Fixed desired)
    {
        return FixedRotationTurnStepResolver.ResolveStep(
            current,
            desired,
            TankCatalog.BasicTank.Stats.TurretTurnRatePerTick).FinalRotation;
    }

    private static FixedVec2 ExpectedVelocityFromRotation(Fixed rotation)
    {
        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(rotation);

        Assert.True(forwardResult.IsResolved);
        return forwardResult.Forward!.Value
            * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick;
    }

    private static void AssertFireRequestMatchesRuntime(
        MatchSensorRuntimeState runtime,
        int tankIndex,
        MatchFireRequest fireRequest)
    {
        FireMuzzlePositionResult muzzleResult = FireMuzzlePositionResolver.Resolve(
            runtime,
            tankIndex,
            new WeaponSlot(0));
        FireVelocityResult velocityResult = FireVelocityResolver.Resolve(
            runtime,
            tankIndex,
            new WeaponSlot(0));

        Assert.Equal(FireMuzzlePositionStatus.Resolved, muzzleResult.Status);
        Assert.Equal(FireVelocityStatus.Resolved, velocityResult.Status);
        Assert.Equal(muzzleResult.MuzzlePosition, fireRequest.MuzzlePosition);
        Assert.Equal(velocityResult.FireVelocity, fireRequest.FireVelocity);
    }

    private static (
        MatchSensorRuntimeState PostTurretRuntime,
        ScriptMappedFireRequestConstructionPipelineResult Construction)
        ApplyAimThenConstructFire(MatchSensorRuntimeState runtime)
    {
        MatchScriptDomainRequestMappingResult aimMapping = MapAimForFirstTankOnly(runtime);

        ScriptMappedTurretRequestApplicationResult turretApply =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, aimMapping);

        MatchSensorRuntimeState postTurretRuntime = turretApply.FinalRuntime;
        MatchScriptDomainRequestMappingResult fireMapping =
            MapPrograms(postTurretRuntime, FireWhenAlways(), NoOpWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(postTurretRuntime, fireMapping, new ProjectileIdSequence(0));

        return (postTurretRuntime, construction);
    }

    private static ScriptMappedFireRequestApplicationResult ApplyPipeline(
        MatchSensorRuntimeState runtime,
        ScriptMappedFireRequestConstructionPipelineResult constructionPipelineResult)
    {
        return ScriptMappedFireRequestApplicationPipeline.Apply(
            runtime,
            constructionPipelineResult);
    }

    private static ScriptMappedFireRequestConstructionPipelineResult ConstructPipeline(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mapping,
        ProjectileIdSequence sequence)
    {
        return ScriptMappedFireRequestConstructionPipeline.Construct(
            runtime,
            mapping,
            sequence);
    }

    #region Pipeline result

    [Fact]
    public void PipelineResult_stores_construction_result_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());
        ProjectileIdSequence sequence = new ProjectileIdSequence(0);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, sequence);

        ScriptMappedFireRequestConstructionResult construction = pipeline.ConstructionResult;

        Assert.NotNull(construction);
        Assert.Equal(1, construction.Count);
        Assert.Same(mapping, construction.MappingResult);
    }

    [Fact]
    public void PipelineResult_stores_final_projectile_sequence()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());
        ProjectileIdSequence sequence = new ProjectileIdSequence(42);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, sequence);

        Assert.Equal(43, pipeline.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void PipelineResult_rejects_null_constructionResult_ParamName_constructionResult()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestConstructionPipelineResult(
                null!,
                new ProjectileIdSequence(0)));

        Assert.Equal("constructionResult", ex.ParamName);
    }

    #endregion

    #region Pipeline validation

    [Fact]
    public void Construct_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestConstructionPipeline.Construct(
                null!,
                mapping,
                new ProjectileIdSequence(0)));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Construct_NullMappingResult_ThrowsArgumentNullException_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestConstructionPipeline.Construct(
                runtime,
                null!,
                new ProjectileIdSequence(0)));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Construct_RuntimeDifferentFromIntegrationRuntime_DoesNotThrow()
    {
        MatchSensorRuntimeState integrationRuntime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(integrationRuntime, FireWhenAlways());

        MatchState postTurretState = integrationRuntime.State.WithCurrentTick(new SimTick(11));
        MatchSensorRuntimeState postTurretRuntime =
            integrationRuntime.WithState(postTurretState);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                postTurretRuntime,
                mapping,
                new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.NotSame(integrationRuntime, postTurretRuntime);
    }

    [Fact]
    public void Construct_AfterTurretApply_UsesPostTurretRotation_ForFireVelocity()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime();
        MatchScriptDomainRequestMappingResult aimMapping = MapAimForFirstTankOnly(runtime);

        ScriptMappedTurretRequestApplicationResult turretApply =
            ScriptMappedTurretRequestApplicationPipeline.Apply(runtime, aimMapping);

        FixedVec2 ownerPosition = FixedVec2.FromInts(10, 20);
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed initialTurret = Fixed.FromRatio(1, 4);
        Fixed desired = FixedRotationInverseLookup.ResolveFromDirection(
            enemyPosition - ownerPosition).Rotation;
        Fixed expectedTurret = FixedRotationTurnStepResolver.ResolveStep(
            initialTurret,
            desired,
            TankCatalog.BasicTank.Stats.TurretTurnRatePerTick).FinalRotation;

        Assert.Equal(expectedTurret, turretApply.FinalRuntime.State.Tanks[0].TurretRotation);

        MatchScriptDomainRequestMappingResult fireMapping =
            MapPrograms(turretApply.FinalRuntime, FireWhenAlways(), NoOpWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult construction =
            ScriptMappedFireRequestConstructionPipeline.Construct(
                turretApply.FinalRuntime,
                fireMapping,
                new ProjectileIdSequence(0));

        MatchFireRequest fireRequest =
            construction.ConstructionResult.Records[0].FireRequest!.Value;

        FixedVec2 expectedForward = FixedRotationDirectionResolver.ResolveForward(expectedTurret)
            .Forward!.Value;
        FixedVec2 expectedVelocity =
            expectedForward * WeaponCatalog.StandardCannon.ProjectileSpeedPerTick;

        Assert.Equal(expectedVelocity, fireRequest.FireVelocity);
    }

    #endregion

    #region Classification

    [Fact]
    public void Construct_None_maps_to_NotWeapon()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, NoOpWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(ScriptDomainRequestCategory.None, mapping.Records[0].Category);
    }

    [Fact]
    public void Construct_Sensor_maps_to_NotWeapon()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(ScriptDomainRequestCategory.Sensor, mapping.Records[0].Category);
    }

    [Fact]
    public void Construct_Turret_maps_to_NotWeapon()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, AimWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(ScriptDomainRequestCategory.Turret, mapping.Records[0].Category);
    }

    [Fact]
    public void Construct_Movement_maps_to_NotWeapon()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, RetreatWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(ScriptDomainRequestCategory.Movement, mapping.Records[0].Category);
    }

    [Fact]
    public void Construct_Unsupported_maps_to_Unsupported()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Fire, string.Empty);
        ScriptCommandTranslationOutput unsupportedOutput = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.UnsupportedCommand(0, cmd, "no"),
            ScriptTranslatedCommandRequest.None());

        ScriptDomainRequestMappingRecord unsupported =
            ScriptTranslatedCommandDomainMapper.Map(
                new MatchScriptIntentIntegrationRecord(
                    integration.Records[0].TankIndex,
                    integration.Records[0].TankId,
                    integration.Records[0].Context,
                    integration.Records[0].EvaluationRecord,
                    unsupportedOutput));

        Assert.Equal(ScriptDomainRequestCategory.Unsupported, unsupported.Category);

        var remapped = new MatchScriptDomainRequestMappingResult(
            integration,
            new[] { unsupported });

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, remapped, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Unsupported,
            pipeline.ConstructionResult.Records[0].Status);
    }

    [Fact]
    public void Construct_WeaponFire_WithResolvedMuzzleAndVelocity_ReturnsConstructed()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());

        Assert.Equal(ScriptDomainRequestCategory.Weapon, mapping.Records[0].Category);
        Assert.Equal(
            ScriptTranslatedCommandRequestKind.Fire,
            mapping.Records[0].TranslationOutput.Request.Kind);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(42));

        ScriptMappedFireRequestConstructionRecord record =
            pipeline.ConstructionResult.Records[0];

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.True(record.DidConstruct);
        Assert.NotNull(record.FireRequest);

        MatchFireRequest fireRequest = record.FireRequest!.Value;
        Assert.Equal(mapping.Records[0].TankId, fireRequest.ShooterTankId);
        Assert.Equal(new WeaponSlot(0), fireRequest.WeaponSlot);
        Assert.Equal(new ProjectileId(42), fireRequest.ProjectileId);
        Assert.Equal(FixedVec2.FromInts(11, 20), fireRequest.MuzzlePosition);
        Assert.Equal(FixedVec2.FromInts(1, 0), fireRequest.FireVelocity);
        Assert.Equal(43, pipeline.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void Construct_Weapon_non_fire_maps_to_NoFireCommand()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult aimIntegration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { AimWhenAlways() });

        ScriptCommandTranslationOutput aimOutput =
            aimIntegration.Records[0].TranslationOutput;

        MatchScriptIntentIntegrationResult fireIntegration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptDomainRequestMappingRecord weaponButAim =
            ScriptDomainRequestMappingRecord.Weapon(
                fireIntegration.Records[0].TankIndex,
                fireIntegration.Records[0].TankId,
                aimOutput,
                fireRequest: null);

        var remapped = new MatchScriptDomainRequestMappingResult(
            fireIntegration,
            new[] { weaponButAim });

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, remapped, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NoFireCommand,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Null(pipeline.ConstructionResult.Records[0].FireRequest);
    }

    #endregion

    #region Batch behavior

    [Fact]
    public void Construct_output_count_equals_mapping_count()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways(), FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(mapping.Count, pipeline.ConstructionResult.Count);
        Assert.Equal(2, pipeline.ConstructionResult.Count);
    }

    [Fact]
    public void Construct_preserves_record_order()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways(), FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(0, pipeline.ConstructionResult.Records[0].RecordIndex);

        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.Constructed,
            pipeline.ConstructionResult.Records[1].Status);
        Assert.Equal(1, pipeline.ConstructionResult.Records[1].RecordIndex);
        Assert.True(pipeline.ConstructionResult.Records[1].DidConstruct);
    }

    [Fact]
    public void Construct_scan_only_does_not_advance_projectile_sequence()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways());
        ProjectileIdSequence input = new ProjectileIdSequence(42);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, input);

        Assert.Equal(input, pipeline.FinalProjectileIdSequence);
        Assert.Equal(42, input.NextValue);
    }

    [Fact]
    public void Construct_fire_advances_projectile_sequence_by_one()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());
        ProjectileIdSequence input = new ProjectileIdSequence(42);

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, input);

        Assert.Equal(43, pipeline.FinalProjectileIdSequence.NextValue);
        Assert.Equal(42, input.NextValue);
    }

    [Fact]
    public void Construct_does_not_mutate_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        MatchSensorRuntimeState runtimeBefore = integration.Runtime;

        ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Same(runtimeBefore, integration.Runtime);
        Assert.Same(runtime, integration.Runtime);
    }

    [Fact]
    public void Construct_preserves_mapping_result_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.Same(mapping, pipeline.ConstructionResult.MappingResult);
    }

    [Fact]
    public void Construct_scan_record_has_null_fire_request_fire_record_constructed()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways(), FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        Assert.False(pipeline.ConstructionResult.Records[0].DidConstruct);
        Assert.Null(pipeline.ConstructionResult.Records[0].FireRequest);

        Assert.True(pipeline.ConstructionResult.Records[1].DidConstruct);
        Assert.NotNull(pipeline.ConstructionResult.Records[1].FireRequest);
    }

    [Fact]
    public void Construct_two_fire_records_allocate_projectile_ids_in_order()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, FireWhenAlways(), FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(42));

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, pipeline.ConstructionResult.Records[0].Status);
        Assert.Equal(new ProjectileId(42), pipeline.ConstructionResult.Records[0].FireRequest!.Value.ProjectileId);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, pipeline.ConstructionResult.Records[1].Status);
        Assert.Equal(new ProjectileId(43), pipeline.ConstructionResult.Records[1].FireRequest!.Value.ProjectileId);

        Assert.Equal(44, pipeline.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void Construct_mixed_order_four_records_allocates_only_constructed_rows()
    {
        MatchSensorRuntimeState runtime = CreateFourTankRuntime();
        MatchScriptDomainRequestMappingResult mapping = MapPrograms(
            runtime,
            ScanWhenAlways(),
            FireWhenAlways(),
            NoOpWhenAlways(),
            FireWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult pipeline =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(42));

        Assert.Equal(4, pipeline.ConstructionResult.Count);
        Assert.Equal(ScriptMappedFireRequestConstructionStatus.NotWeapon, pipeline.ConstructionResult.Records[0].Status);
        Assert.Null(pipeline.ConstructionResult.Records[0].FireRequest);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, pipeline.ConstructionResult.Records[1].Status);
        Assert.Equal(new ProjectileId(42), pipeline.ConstructionResult.Records[1].FireRequest!.Value.ProjectileId);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.NotWeapon, pipeline.ConstructionResult.Records[2].Status);
        Assert.Null(pipeline.ConstructionResult.Records[2].FireRequest);

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, pipeline.ConstructionResult.Records[3].Status);
        Assert.Equal(new ProjectileId(43), pipeline.ConstructionResult.Records[3].FireRequest!.Value.ProjectileId);

        Assert.Equal(44, pipeline.FinalProjectileIdSequence.NextValue);
    }

    [Fact]
    public void Construct_repeated_calls_produce_equivalent_statuses_and_sequence()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways(), FireWhenAlways());
        ProjectileIdSequence sequence = new ProjectileIdSequence(7);

        ScriptMappedFireRequestConstructionPipelineResult a =
            ConstructPipeline(runtime, mapping, sequence);
        ScriptMappedFireRequestConstructionPipelineResult b =
            ConstructPipeline(runtime, mapping, sequence);

        Assert.Equal(a.FinalProjectileIdSequence, b.FinalProjectileIdSequence);

        for (int i = 0; i < mapping.Count; i++)
        {
            Assert.Equal(
                a.ConstructionResult.Records[i].Status,
                b.ConstructionResult.Records[i].Status);
            Assert.Equal(
                a.ConstructionResult.Records[i].FireRequest,
                b.ConstructionResult.Records[i].FireRequest);
        }
    }

    #endregion

    #region Fire construction uses current turret rotation (5.146)

    [Fact]
    public void Construct_AfterPartialTurretTurn_UsesSteppedRotation_NotDesiredRotation()
    {
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(ownerPosition, enemyPosition);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed steppedRotation = ExpectedTurnStep(Fixed.Zero, desiredRotation);

        (MatchSensorRuntimeState postTurretRuntime, ScriptMappedFireRequestConstructionPipelineResult construction) =
            ApplyAimThenConstructFire(runtime);

        ScriptMappedFireRequestConstructionRecord record =
            construction.ConstructionResult.Records[0];

        Assert.Equal(124, desiredRotation.Raw);
        Assert.Equal(100, steppedRotation.Raw);
        Assert.NotEqual(desiredRotation, steppedRotation);
        Assert.Equal(steppedRotation, postTurretRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.True(record.DidConstruct);
        Assert.NotNull(record.FireRequest);

        MatchFireRequest fireRequest = record.FireRequest!.Value;
        AssertFireRequestMatchesRuntime(postTurretRuntime, tankIndex: 0, fireRequest);
        Assert.NotEqual(
            ExpectedVelocityFromRotation(desiredRotation),
            fireRequest.FireVelocity);
    }

    [Fact]
    public void Construct_WhenTurnStepReachesDesired_UsesDesiredRotationForFireGeometry()
    {
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 10);
        Fixed initialTurret = Fixed.FromRaw(50);
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            ownerPosition,
            enemyPosition,
            initialTurret);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);
        Fixed steppedRotation = ExpectedTurnStep(initialTurret, desiredRotation);

        (MatchSensorRuntimeState postTurretRuntime, ScriptMappedFireRequestConstructionPipelineResult construction) =
            ApplyAimThenConstructFire(runtime);

        ScriptMappedFireRequestConstructionRecord record =
            construction.ConstructionResult.Records[0];

        Assert.Equal(Fixed.Zero, desiredRotation);
        Assert.Equal(desiredRotation, steppedRotation);
        Assert.Equal(Fixed.Zero, postTurretRuntime.State.Tanks[0].TurretRotation);
        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.True(record.DidConstruct);

        MatchFireRequest fireRequest = record.FireRequest!.Value;
        AssertFireRequestMatchesRuntime(postTurretRuntime, tankIndex: 0, fireRequest);
        Assert.Equal(FixedVec2.FromInts(1, 0), fireRequest.FireVelocity);
    }

    [Fact]
    public void Construct_WithUnchangedCurrentRotation_FireUsesRuntimeTurretOnly()
    {
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        Fixed initialTurret = Fixed.FromRatio(1, 8);
        MatchSensorRuntimeState runtime = CreateTwoTankAimRuntime(
            ownerPosition,
            enemyPosition,
            initialTurret);
        Fixed desiredRotation = ExpectedAimRotation(ownerPosition, enemyPosition);

        Assert.Equal(initialTurret, runtime.State.Tanks[0].TurretRotation);
        Assert.NotEqual(Fixed.Zero, initialTurret);
        Assert.NotEqual(desiredRotation, initialTurret);

        MatchScriptDomainRequestMappingResult fireMapping =
            MapPrograms(runtime, FireWhenAlways(), NoOpWhenAlways());

        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, fireMapping, new ProjectileIdSequence(0));

        ScriptMappedFireRequestConstructionRecord record =
            construction.ConstructionResult.Records[0];

        Assert.Equal(ScriptMappedFireRequestConstructionStatus.Constructed, record.Status);
        Assert.True(record.DidConstruct);
        Assert.NotEqual(ScriptMappedFireRequestConstructionStatus.TurretNotAligned, record.Status);
        Assert.NotNull(record.FireRequest);

        MatchFireRequest fireRequest = record.FireRequest!.Value;
        AssertFireRequestMatchesRuntime(runtime, tankIndex: 0, fireRequest);
        Assert.NotEqual(
            ExpectedVelocityFromRotation(desiredRotation),
            fireRequest.FireVelocity);
    }

    [Fact]
    public void Apply_ConstructedFireRequestWhileTurning_CreatesProjectileWithCurrentRotationVelocity()
    {
        FixedVec2 ownerPosition = FullAngleOwner;
        FixedVec2 enemyPosition = FixedVec2.FromInts(20, 20);
        MatchSensorRuntimeState runtime =
            CreateTwoTankAimRuntime(ownerPosition, enemyPosition);

        (MatchSensorRuntimeState postTurretRuntime, ScriptMappedFireRequestConstructionPipelineResult construction) =
            ApplyAimThenConstructFire(runtime);

        ScriptMappedFireRequestApplicationResult applied =
            ApplyPipeline(postTurretRuntime, construction);

        MatchFireRequest fireRequest =
            construction.ConstructionResult.Records[0].FireRequest!.Value;

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.True(applied.Records[0].DidApply);
        Assert.Single(applied.FinalRuntime.State.Projectiles);

        ProjectileState projectile = applied.FinalRuntime.State.Projectiles[0];
        Assert.Equal(fireRequest.MuzzlePosition, projectile.Position);
        Assert.Equal(fireRequest.FireVelocity, projectile.VelocityPerTick);
        Assert.Equal(new TankId(0), projectile.OwnerTankId);
        Assert.Equal(new TankId(0), fireRequest.ShooterTankId);
    }

    #endregion
}
