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

public sealed class ScriptMappedMovementRequestApplicationPipelineTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        Fixed bodyRotation = default,
        Fixed turretRotation = default)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
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
        Fixed bodyRotation = default,
        Fixed turretRotation = default)
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[] { CreateWeaponLoadout() },
            CreateTank(0, 0, bodyRotation, turretRotation));

        return CreateRuntime(state, CreateSensorLoadout());
    }

    private static MatchSensorRuntimeState CreateTwoTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[] { CreateWeaponLoadout(), CreateWeaponLoadout() },
            CreateTank(0, 0),
            CreateTank(1, 1));

        return CreateRuntime(
            state,
            CreateSensorLoadout(),
            CreateSensorLoadout());
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

    private static MatchScriptDomainRequestMappingResult MapPrograms(
        MatchSensorRuntimeState runtime,
        params ScriptProgram[] programs)
    {
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
    }

    private static ScriptMappedMovementRequestApplicationResult ApplyMovement(
        MatchSensorRuntimeState runtime,
        MatchScriptDomainRequestMappingResult mapping)
    {
        return ScriptMappedMovementRequestApplicationPipeline.Apply(runtime, mapping);
    }

    private static ScriptCommandTranslationOutput MovementTranslationOutput(
        ScriptTranslatedCommandRequestKind kind,
        string payload)
    {
        ScriptCommandType commandType = kind switch
        {
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint =>
                ScriptCommandType.MoveToPatrolPoint,
            ScriptTranslatedCommandRequestKind.Retreat => ScriptCommandType.Retreat,
            _ => ScriptCommandType.Fire,
        };

        ScriptCommand command = new ScriptCommand(commandType, payload);

        ScriptCommandTranslationResult result =
            ScriptCommandTranslationResult.Translated(
                routineIndex: 0,
                command,
                message: "Movement translation for test.");

        ScriptTranslatedCommandRequest request =
            ScriptTranslatedCommandRequest.Create(kind, routineIndex: 0, command, payload);

        return new ScriptCommandTranslationOutput(result, request);
    }

    private static ScriptCommandTranslationOutput FireTranslationOutput()
    {
        return MovementTranslationOutput(
            ScriptTranslatedCommandRequestKind.Fire,
            string.Empty);
    }

    private static FixedVec2 ExpectedVelocity(TankState tank, ScriptTranslatedCommandRequestKind kind)
    {
        FixedRotationDirectionResult forwardResult =
            FixedRotationDirectionResolver.ResolveForward(tank.BodyRotation);

        Assert.True(forwardResult.IsResolved);

        FixedVec2 forward = forwardResult.Forward!.Value;
        Fixed maxSpeed = tank.Definition.Stats.MaxVelocityPerTick;

        return kind switch
        {
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint => forward * maxSpeed,
            ScriptTranslatedCommandRequestKind.Retreat => -forward * maxSpeed,
            _ => throw new InvalidOperationException("Unexpected movement kind for test."),
        };
    }

    private static void AssertPositionUnchanged(TankState before, TankState after)
    {
        Assert.Equal(before.Movement.Position, after.Movement.Position);
    }

    private static MatchScriptDomainRequestMappingResult BuildSameTankDoubleMovementMapping(
        MatchSensorRuntimeState runtime)
    {
        MatchScriptIntentIntegrationResult single =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() });

        MatchScriptIntentIntegrationRecord integrationRecord = single.GetRecordAtIndex(0);

        var integration = new MatchScriptIntentIntegrationResult(
            runtime,
            new[] { integrationRecord, integrationRecord });

        ScriptCommandTranslationOutput patrolTranslation = integrationRecord.TranslationOutput;
        ScriptCommandTranslationOutput retreatTranslation =
            MovementTranslationOutput(
                ScriptTranslatedCommandRequestKind.Retreat,
                "away_from_nearest_visible");

        TankId tankId = integrationRecord.TankId;

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.MovementPlaceholder(0, tankId, patrolTranslation),
            ScriptDomainRequestMappingRecord.MovementPlaceholder(0, tankId, retreatTranslation),
        };

        return new MatchScriptDomainRequestMappingResult(integration, mappingRecords);
    }

    private static MatchScriptDomainRequestMappingResult BuildRejectedInvalidMovementMapping(
        MatchSensorRuntimeState runtime)
    {
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() });

        MatchScriptIntentIntegrationRecord integrationRecord = integration.GetRecordAtIndex(0);

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.MovementPlaceholder(
                0,
                integrationRecord.TankId,
                FireTranslationOutput()),
        };

        return new MatchScriptDomainRequestMappingResult(integration, mappingRecords);
    }

    #region Validation

    [Fact]
    public void Apply_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, MoveToPatrolPointWhenAlways());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedMovementRequestApplicationPipeline.Apply(null!, mapping));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Apply_NullMappingResult_ThrowsArgumentNullException_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedMovementRequestApplicationPipeline.Apply(runtime, null!));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Apply_RuntimeDifferentFromIntegrationRuntime_DoesNotThrow()
    {
        MatchSensorRuntimeState integrationRuntime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(integrationRuntime, MoveToPatrolPointWhenAlways());

        MatchState postFireState = integrationRuntime.State.WithCurrentTick(new SimTick(11));
        MatchSensorRuntimeState postFireRuntime =
            integrationRuntime.WithState(postFireState);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(postFireRuntime, mapping);

        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.NotSame(integrationRuntime, postFireRuntime);
        Assert.NotSame(
            mapping.IntegrationResult.Runtime,
            applied.FinalRuntime);
    }

    #endregion

    #region Applied

    [Fact]
    public void Apply_MoveToPatrolPoint_SetsVelocityPerTick_FromBodyForward()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(bodyRotation: Fixed.Zero);
        TankState tankBefore = runtime.State.Tanks[0];
        FixedVec2 expected = ExpectedVelocity(
            tankBefore,
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        TankState tankAfter = applied.FinalRuntime.State.Tanks[0];

        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.Equal(expected, tankAfter.Movement.VelocityPerTick);
        Assert.Equal(expected, applied.Records[0].AppliedVelocity);
        AssertPositionUnchanged(tankBefore, tankAfter);
    }

    [Fact]
    public void Apply_Retreat_SetsVelocityPerTick_OppositeBodyForward()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(bodyRotation: Fixed.Zero);
        TankState tankBefore = runtime.State.Tanks[0];
        FixedVec2 expected = ExpectedVelocity(
            tankBefore,
            ScriptTranslatedCommandRequestKind.Retreat);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, RetreatWhenAlways()));

        TankState tankAfter = applied.FinalRuntime.State.Tanks[0];

        Assert.Equal(expected, tankAfter.Movement.VelocityPerTick);
        AssertPositionUnchanged(tankBefore, tankAfter);
    }

    [Fact]
    public void Apply_QuarterBodyRotation_SetsVelocityFromBodyForward()
    {
        Fixed bodyRotation = Fixed.FromRatio(1, 4);
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(bodyRotation: bodyRotation);
        TankState tankBefore = runtime.State.Tanks[0];
        FixedVec2 expected = ExpectedVelocity(
            tankBefore,
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.Equal(expected, applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        AssertPositionUnchanged(tankBefore, applied.FinalRuntime.State.Tanks[0]);
    }

    [Fact]
    public void Apply_TurretRotationIgnored_UsesBodyRotation()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.FromRatio(1, 4));

        TankState tankBefore = runtime.State.Tanks[0];
        FixedVec2 bodyBased = ExpectedVelocity(
            tankBefore,
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint);

        FixedVec2 northIfBodyMatchedTurret = ExpectedVelocity(
            tankBefore.WithBodyRotation(Fixed.FromRatio(1, 4)),
            ScriptTranslatedCommandRequestKind.MoveToPatrolPoint);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.Equal(bodyBased, applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        Assert.NotEqual(
            northIfBodyMatchedTurret,
            applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_AppliedRecord_HasDidApplyTrue()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.True(applied.Records[0].DidApply);
    }

    #endregion

    #region Skipped and failures

    [Fact]
    public void Apply_NonMovementRecord_ReturnsSkippedNotMovement()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, ScanWhenAlways()));

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement,
            applied.Records[0].Status);
        Assert.False(applied.Records[0].DidApply);
        Assert.Same(runtime.State, applied.FinalRuntime.State);
    }

    [Fact]
    public void Apply_TankIndexOutOfRange_ReturnsTankIndexOutOfRange()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { MoveToPatrolPointWhenAlways() });

        MatchScriptIntentIntegrationRecord record = integration.GetRecordAtIndex(0);

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.MovementPlaceholder(
                tankIndex: 99,
                record.TankId,
                record.TranslationOutput),
        };

        MatchScriptDomainRequestMappingResult mapping =
            new MatchScriptDomainRequestMappingResult(integration, mappingRecords);

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.TankIndexOutOfRange,
            applied.Records[0].Status);
    }

    [Fact]
    public void Apply_DestroyedTank_ReturnsTankDestroyed()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        TankState destroyed = runtime.State.Tanks[0].WithCurrentHitPoints(0);
        MatchState state = runtime.State.WithTanks(new[] { destroyed });
        MatchSensorRuntimeState destroyedRuntime = runtime.WithState(state);

        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(
                destroyedRuntime,
                MapPrograms(destroyedRuntime, MoveToPatrolPointWhenAlways()));

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.TankDestroyed,
            applied.Records[0].Status);
        Assert.Equal(FixedVec2.Zero, applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_RejectedInvalidMovement_ManualMapping()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            BuildRejectedInvalidMovementMapping(runtime);

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.RejectedInvalidMovement,
            applied.Records[0].Status);
        Assert.Same(runtime.State, applied.FinalRuntime.State);
    }

    #endregion

    #region Ordering and threading

    [Fact]
    public void Apply_SameTankTwice_SecondVelocityWins()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime(bodyRotation: Fixed.Zero);
        MatchScriptDomainRequestMappingResult mapping =
            BuildSameTankDoubleMovementMapping(runtime);

        TankState tankBefore = runtime.State.Tanks[0];
        FixedVec2 retreatVelocity = ExpectedVelocity(
            tankBefore,
            ScriptTranslatedCommandRequestKind.Retreat);

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            applied.Records[0].Status);
        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            applied.Records[1].Status);
        Assert.Equal(retreatVelocity, applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
        AssertPositionUnchanged(tankBefore, applied.FinalRuntime.State.Tanks[0]);
    }

    [Fact]
    public void Apply_TwoTanks_BothApplied()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(
                runtime,
                MapPrograms(runtime, MoveToPatrolPointWhenAlways(), RetreatWhenAlways()));

        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.Applied, applied.Records[0].Status);
        Assert.Equal(ScriptMappedMovementRequestApplicationStatus.Applied, applied.Records[1].Status);
        Assert.NotEqual(
            applied.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick,
            applied.FinalRuntime.State.Tanks[1].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_MixedBatch_ReturnsExpectedStatusesInOrder()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    MoveToPatrolPointWhenAlways(),
                });

        MatchScriptIntentIntegrationRecord patrolRecord = integration.GetRecordAtIndex(1);

        var mappingRecords = new[]
        {
            ScriptDomainRequestMappingRecord.Sensor(
                0,
                integration.GetRecordAtIndex(0).TankId,
                integration.GetRecordAtIndex(0).TranslationOutput,
                new MatchSensorScanRequest(0, SensorSlot.Zero)),
            ScriptDomainRequestMappingRecord.MovementPlaceholder(
                1,
                patrolRecord.TankId,
                patrolRecord.TranslationOutput),
        };

        MatchScriptDomainRequestMappingResult mapping =
            new MatchScriptDomainRequestMappingResult(integration, mappingRecords);

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.SkippedNotMovement,
            applied.Records[0].Status);
        Assert.Equal(
            ScriptMappedMovementRequestApplicationStatus.Applied,
            applied.Records[1].Status);
    }

    [Fact]
    public void Apply_OutputCountMatchesMappingCount()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, MoveToPatrolPointWhenAlways(), RetreatWhenAlways());

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Equal(mapping.Count, applied.Count);
    }

    [Fact]
    public void Apply_GetRecordAtIndex_ReturnsExpectedRecord()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.Same(applied.Records[0], applied.GetRecordAtIndex(0));
        Assert.Equal(0, applied.GetRecordAtIndex(0).RecordIndex);
    }

    #endregion

    #region Purity

    [Fact]
    public void Apply_PreservesSensorLoadoutsReference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        ScriptMappedMovementRequestApplicationResult applied =
            ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.Same(runtime.SensorLoadouts, applied.FinalRuntime.SensorLoadouts);
    }

    [Fact]
    public void Apply_DoesNotMutateInputRuntimeState()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchState stateBefore = runtime.State;
        FixedVec2 positionBefore = runtime.State.Tanks[0].Movement.Position;
        FixedVec2 velocityBefore = runtime.State.Tanks[0].Movement.VelocityPerTick;

        ApplyMovement(runtime, MapPrograms(runtime, MoveToPatrolPointWhenAlways()));

        Assert.Same(stateBefore, runtime.State);
        Assert.Equal(positionBefore, runtime.State.Tanks[0].Movement.Position);
        Assert.Equal(velocityBefore, runtime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_RepeatedCalls_EquivalentResults()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, MoveToPatrolPointWhenAlways());

        ScriptMappedMovementRequestApplicationResult a = ApplyMovement(runtime, mapping);
        ScriptMappedMovementRequestApplicationResult b = ApplyMovement(runtime, mapping);

        Assert.Equal(a.Records[0].Status, b.Records[0].Status);
        Assert.Equal(a.Records[0].AppliedVelocity, b.Records[0].AppliedVelocity);
        Assert.Equal(
            a.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick,
            b.FinalRuntime.State.Tanks[0].Movement.VelocityPerTick);
    }

    [Fact]
    public void Apply_PreservesMappingResultReference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptDomainRequestMappingResult mapping =
            MapPrograms(runtime, MoveToPatrolPointWhenAlways());

        ScriptMappedMovementRequestApplicationResult applied = ApplyMovement(runtime, mapping);

        Assert.Same(mapping, applied.MappingResult);
    }

    #endregion
}
