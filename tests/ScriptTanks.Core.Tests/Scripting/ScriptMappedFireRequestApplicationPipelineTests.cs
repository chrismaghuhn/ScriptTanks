using System;
using System.Collections.Generic;
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

public sealed class ScriptMappedFireRequestApplicationPipelineTests
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

    private static TankWeaponLoadout CreateFiredLoadout(SimTick lastFireTick)
    {
        return new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon).MarkFired(lastFireTick),
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

    private static MatchSensorRuntimeState CreateSingleTankRuntimeWithFiredWeapon(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[] { CreateFiredLoadout(tick) },
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

    private static ScriptMappedFireRequestApplicationResult ApplyPipeline(
        MatchSensorRuntimeState runtime,
        ScriptMappedFireRequestConstructionPipelineResult constructionPipelineResult)
    {
        return ScriptMappedFireRequestApplicationPipeline.Apply(
            runtime,
            constructionPipelineResult);
    }

    private static ScriptMappedFireRequestApplicationResult ConstructAndApply(
        MatchSensorRuntimeState runtime,
        params ScriptProgram[] programs)
    {
        MatchScriptDomainRequestMappingResult mapping = MapPrograms(runtime, programs);
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        return ApplyPipeline(runtime, construction);
    }

    private static ScriptMappedFireRequestConstructionPipelineResult BuildSameTankDoubleFirePipelineResult(
        MatchSensorRuntimeState runtime)
    {
        MatchScriptIntentIntegrationResult single =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptIntentIntegrationRecord integrationRecord = single.GetRecordAtIndex(0);

        var integration = new MatchScriptIntentIntegrationResult(
            runtime,
            new[] { integrationRecord, integrationRecord });

        ScriptCommandTranslationOutput translation = integrationRecord.TranslationOutput;
        TankId tankId = integrationRecord.TankId;

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.Weapon(0, tankId, translation, fireRequest: null),
            ScriptDomainRequestMappingRecord.Weapon(0, tankId, translation, fireRequest: null),
        };

        MatchScriptDomainRequestMappingResult mapping =
            new MatchScriptDomainRequestMappingResult(integration, mappingRecords);

        return ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));
    }

    #region Validation

    [Fact]
    public void Apply_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestApplicationPipeline.Apply(null!, construction));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Apply_NullConstructionPipelineResult_ThrowsArgumentNullException_ParamName_constructionPipelineResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedFireRequestApplicationPipeline.Apply(runtime, null!));

        Assert.Equal("constructionPipelineResult", ex.ParamName);
    }

    [Fact]
    public void Apply_RuntimeDifferentFromIntegrationRuntime_DoesNotThrow()
    {
        MatchSensorRuntimeState integrationRuntime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                integrationRuntime,
                MapPrograms(integrationRuntime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        MatchState postTurretState = integrationRuntime.State.WithCurrentTick(new SimTick(11));
        MatchSensorRuntimeState postTurretRuntime =
            integrationRuntime.WithState(postTurretState);

        ScriptMappedFireRequestApplicationResult applied =
            ApplyPipeline(postTurretRuntime, construction);

        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.NotSame(integrationRuntime, postTurretRuntime);
    }

    #endregion

    #region Skipped

    [Fact]
    public void Apply_NonConstructedRecord_ReturnsSkippedNotConstructed()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, ScanWhenAlways());
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied =
            ApplyPipeline(runtime, construction);

        ScriptMappedFireRequestApplicationRecord record = applied.Records[0];

        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            record.Status);
    }

    [Fact]
    public void Apply_SkippedRecord_HasDidApplyFalse()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, ScanWhenAlways());

        Assert.False(applied.Records[0].DidApply);
    }

    [Fact]
    public void Apply_SkippedRecord_HasNullFireOutcome()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, NoOpWhenAlways());

        Assert.Null(applied.Records[0].FireOutcome);
    }

    [Fact]
    public void Apply_SkippedRecord_DoesNotChangeFinalRuntimeState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchState stateBefore = runtime.State;

        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, ScanWhenAlways());

        Assert.Same(stateBefore, applied.FinalRuntime.State);
        Assert.Empty(applied.FinalRuntime.State.Projectiles);
    }

    #endregion

    #region Applied

    [Fact]
    public void Apply_ConstructedFire_ReturnsApplied()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[0].Status);
    }

    [Fact]
    public void Apply_AppliedRecord_HasDidApplyTrue()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.True(applied.Records[0].DidApply);
    }

    [Fact]
    public void Apply_AppliedRecord_HasNonNullFireOutcomeWithDidFireTrue()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.NotNull(applied.Records[0].FireOutcome);
        Assert.True(applied.Records[0].FireOutcome!.DidFire);
    }

    [Fact]
    public void Apply_AppliedRecord_AppendsProjectileToFinalState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.Single(applied.FinalRuntime.State.Projectiles);
    }

    [Fact]
    public void Apply_AppliedRecord_WeaponLoadoutReflectsFiredState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        WeaponState weapon = applied.FinalRuntime.State.Loadouts[0].Weapons[0];

        Assert.True(weapon.HasFired);
        Assert.Equal(applied.FinalRuntime.State.CurrentTick, weapon.LastFireTick);
    }

    #endregion

    #region Rejected

    [Fact]
    public void Apply_NotReadyWeapon_ReturnsFireRejected()
    {
        SimTick tick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithFiredWeapon(tick);
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied = ApplyPipeline(runtime, construction);

        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.FireRejected,
            applied.Records[0].Status);
    }

    [Fact]
    public void Apply_RejectedRecord_HasDidApplyFalse()
    {
        SimTick tick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithFiredWeapon(tick);
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.False(applied.Records[0].DidApply);
    }

    [Fact]
    public void Apply_RejectedRecord_HasNonNullFireOutcomeWithDidFireFalse()
    {
        SimTick tick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithFiredWeapon(tick);
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.NotNull(applied.Records[0].FireOutcome);
        Assert.False(applied.Records[0].FireOutcome!.DidFire);
    }

    [Fact]
    public void Apply_RejectedRecord_FinalStateMatchesFireOutcomeUpdatedState()
    {
        SimTick tick = new SimTick(10);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntimeWithFiredWeapon(tick);
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.Same(
            applied.Records[0].FireOutcome!.UpdatedState,
            applied.FinalRuntime.State);
    }

    #endregion

    #region Ordering and threading

    [Fact]
    public void Apply_SameTankDoubleFire_FirstAppliedSecondRejected()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            BuildSameTankDoubleFirePipelineResult(runtime);

        ScriptMappedFireRequestApplicationResult applied =
            ApplyPipeline(runtime, construction);

        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.FireRejected,
            applied.Records[1].Status);
    }

    [Fact]
    public void Apply_SameTankDoubleFire_OnlyOneProjectileAppended()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            BuildSameTankDoubleFirePipelineResult(runtime);

        ScriptMappedFireRequestApplicationResult applied =
            ApplyPipeline(runtime, construction);

        Assert.Single(applied.FinalRuntime.State.Projectiles);
        Assert.Equal(new TankId(0), applied.Records[0].ConstructionRecord.FireRequest!.Value.ShooterTankId);
        Assert.Equal(new TankId(0), applied.Records[1].ConstructionRecord.FireRequest!.Value.ShooterTankId);
        Assert.Equal(new WeaponSlot(0), applied.Records[0].ConstructionRecord.FireRequest!.Value.WeaponSlot);
        Assert.Equal(new WeaponSlot(0), applied.Records[1].ConstructionRecord.FireRequest!.Value.WeaponSlot);
    }

    [Fact]
    public void Apply_TwoTanksDualFire_BothAppliedWithTwoProjectiles()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways(), FireWhenAlways());

        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[1].Status);
        Assert.Equal(2, applied.FinalRuntime.State.Projectiles.Count);
        Assert.Equal(new ProjectileId(0), applied.Records[0].ConstructionRecord.FireRequest!.Value.ProjectileId);
        Assert.Equal(new ProjectileId(1), applied.Records[1].ConstructionRecord.FireRequest!.Value.ProjectileId);
    }

    [Fact]
    public void Apply_MixedBatch_ReturnsExpectedStatusesInOrder()
    {
        MatchSensorRuntimeState runtime = CreateFourTankRuntime();
        MatchScriptDomainRequestMappingResult mapping = MapPrograms(
            runtime,
            ScanWhenAlways(),
            FireWhenAlways(),
            NoOpWhenAlways(),
            FireWhenAlways());
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied = ApplyPipeline(runtime, construction);

        Assert.Equal(4, applied.Count);
        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            applied.Records[0].Status);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[1].Status);
        Assert.Equal(
            ScriptMappedFireRequestApplicationStatus.SkippedNotConstructed,
            applied.Records[2].Status);
        Assert.Equal(ScriptMappedFireRequestApplicationStatus.Applied, applied.Records[3].Status);
        Assert.Equal(2, applied.FinalRuntime.State.Projectiles.Count);
    }

    [Fact]
    public void Apply_MixedBatch_GetRecordAtIndexReturnsExpectedRecords()
    {
        MatchSensorRuntimeState runtime = CreateFourTankRuntime();
        MatchScriptDomainRequestMappingResult mapping = MapPrograms(
            runtime,
            ScanWhenAlways(),
            FireWhenAlways(),
            NoOpWhenAlways(),
            FireWhenAlways());
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied = ApplyPipeline(runtime, construction);

        Assert.Same(applied.Records[1], applied.GetRecordAtIndex(1));
        Assert.Equal(3, applied.GetRecordAtIndex(3).RecordIndex);
    }

    [Fact]
    public void Apply_OutputCountEqualsConstructionResultCount()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, ScanWhenAlways(), FireWhenAlways()),
                new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied = ApplyPipeline(runtime, construction);

        Assert.Equal(construction.ConstructionResult.Count, applied.Count);
    }

    #endregion

    #region Purity

    [Fact]
    public void Apply_preserves_constructionPipelineResult_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult applied = ApplyPipeline(runtime, construction);

        Assert.Same(construction, applied.ConstructionPipelineResult);
    }

    [Fact]
    public void Apply_preserves_sensor_loadouts_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestApplicationResult applied =
            ConstructAndApply(runtime, FireWhenAlways());

        Assert.Same(runtime.SensorLoadouts, applied.FinalRuntime.SensorLoadouts);
    }

    [Fact]
    public void Apply_does_not_mutate_input_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(runtime, mapping, new ProjectileIdSequence(0));

        ApplyPipeline(runtime, construction);

        Assert.Same(runtime, integration.Runtime);
    }

    [Fact]
    public void Apply_repeated_calls_same_input_equivalent_statuses_and_projectile_counts()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult construction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        ScriptMappedFireRequestApplicationResult a = ApplyPipeline(runtime, construction);
        ScriptMappedFireRequestApplicationResult b = ApplyPipeline(runtime, construction);

        Assert.Equal(a.Records[0].Status, b.Records[0].Status);
        Assert.Equal(a.Records[0].DidApply, b.Records[0].DidApply);
        Assert.Equal(
            a.FinalRuntime.State.Projectiles.Count,
            b.FinalRuntime.State.Projectiles.Count);
    }

    #endregion

    #region Unknown shooter

    [Fact]
    public void Apply_UnknownShooterTankId_ThrowsInvalidOperationException()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedFireRequestConstructionPipelineResult validConstruction =
            ConstructPipeline(
                runtime,
                MapPrograms(runtime, FireWhenAlways()),
                new ProjectileIdSequence(0));

        ScriptMappedFireRequestConstructionRecord validRecord =
            validConstruction.ConstructionResult.Records[0];

        MatchFireRequest badRequest = new MatchFireRequest(
            new TankId(99),
            validRecord.FireRequest!.Value.WeaponSlot,
            validRecord.FireRequest!.Value.ProjectileId,
            validRecord.FireRequest!.Value.MuzzlePosition,
            validRecord.FireRequest!.Value.FireVelocity);

        ScriptMappedFireRequestConstructionRecord badConstructionRecord =
            ScriptMappedFireRequestConstructionRecord.Constructed(
                0,
                validRecord.MappingRecord,
                badRequest);

        var constructionResult = new ScriptMappedFireRequestConstructionResult(
            validConstruction.ConstructionResult.MappingResult,
            new[] { badConstructionRecord });

        var badPipeline = new ScriptMappedFireRequestConstructionPipelineResult(
            constructionResult,
            new ProjectileIdSequence(0));

        Assert.Throws<InvalidOperationException>(() =>
            ApplyPipeline(runtime, badPipeline));
    }

    #endregion
}
