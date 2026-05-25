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

public sealed class ScriptMappedSensorRequestApplicationPipelineTests
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

    #region ScriptMappedSensorRequestApplicationRecord

    [Fact]
    public void ApplicationRecord_rejects_negative_recordIndex_ParamName_recordIndex()
    {
        ScriptDomainRequestMappingRecord mapping =
            ScriptDomainRequestMappingRecord.None(
                0,
                new TankId(0),
                MinimalTranslationOutput());

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ScriptMappedSensorRequestApplicationRecord(-1, mapping, false, null));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void ApplicationRecord_rejects_null_mappingRecord_ParamName_mappingRecord()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedSensorRequestApplicationRecord(0, null!, true, CreateMinimalScanResult()));

        Assert.Equal("mappingRecord", ex.ParamName);
    }

    [Fact]
    public void ApplicationRecord_rejects_didApply_true_with_null_scanResult_ParamName_scanResult()
    {
        ScriptDomainRequestMappingRecord mapping =
            ScriptDomainRequestMappingRecord.None(
                0,
                new TankId(0),
                MinimalTranslationOutput());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedSensorRequestApplicationRecord(0, mapping, didApply: true, scanResult: null));

        Assert.Equal("scanResult", ex.ParamName);
    }

    [Fact]
    public void ApplicationRecord_rejects_didApply_false_with_non_null_scanResult_ParamName_scanResult()
    {
        ScriptDomainRequestMappingRecord mapping =
            ScriptDomainRequestMappingRecord.None(
                0,
                new TankId(0),
                MinimalTranslationOutput());

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedSensorRequestApplicationRecord(
                0,
                mapping,
                didApply: false,
                scanResult: CreateMinimalScanResult()));

        Assert.Equal("scanResult", ex.ParamName);
    }

    #endregion

    #region ScriptMappedSensorRequestApplicationResult

    private static ScriptCommandTranslationOutput MinimalTranslationOutput()
    {
        return new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.NoIntent(),
            ScriptTranslatedCommandRequest.None());
    }

    private static SensorScanResult CreateMinimalScanResult()
    {
        return new SensorScanResult(
            SensorScanStatus.NoDetection,
            new SimTick(1),
            SensorCatalog.BasicRadar,
            new TankId(0),
            Array.Empty<DetectedTankSnapshot>());
    }

    private static MatchScriptDomainRequestMappingResult MinimalMappingResult(int recordCount = 1)
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(recordCount, mapping.Count);

        return mapping;
    }

    [Fact]
    public void ApplicationResult_rejects_null_mappingResult_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedSensorRequestApplicationResult(
                null!,
                runtime,
                Array.Empty<ScriptMappedSensorRequestApplicationRecord>()));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_rejects_null_finalRuntime_ParamName_finalRuntime()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedSensorRequestApplicationResult(
                mapping,
                finalRuntime: null!,
                Array.Empty<ScriptMappedSensorRequestApplicationRecord>()));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_rejects_null_records_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedSensorRequestApplicationResult(mapping, runtime, null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_rejects_null_record_element_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedSensorRequestApplicationRecord?[] bad =
        {
            new ScriptMappedSensorRequestApplicationRecord(
                0,
                mapping.GetRecordAtIndex(0),
                false,
                null),
            null,
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedSensorRequestApplicationResult(
                mapping,
                CreateSingleTankRuntime(),
                bad!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_rejects_count_mismatch_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedSensorRequestApplicationResult(
                mapping,
                CreateSingleTankRuntime(),
                Array.Empty<ScriptMappedSensorRequestApplicationRecord>()));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_defensively_copies_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedSensorRequestApplicationRecord first =
            new ScriptMappedSensorRequestApplicationRecord(
                0,
                mapping.GetRecordAtIndex(0),
                false,
                null);

        ScriptMappedSensorRequestApplicationRecord[] array = { first };

        var result = new ScriptMappedSensorRequestApplicationResult(
            mapping,
            CreateSingleTankRuntime(),
            array);

        ScriptMappedSensorRequestApplicationRecord replacement =
            new ScriptMappedSensorRequestApplicationRecord(
                0,
                mapping.GetRecordAtIndex(0),
                false,
                null);

        array[0] = replacement;

        Assert.Same(first, result.Records[0]);
    }

    [Fact]
    public void ApplicationResult_Records_is_read_only()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedSensorRequestApplicationRecord record =
            new ScriptMappedSensorRequestApplicationRecord(
                0,
                mapping.GetRecordAtIndex(0),
                false,
                null);

        var result = new ScriptMappedSensorRequestApplicationResult(
            mapping,
            CreateSingleTankRuntime(),
            new[] { record });

        IList<ScriptMappedSensorRequestApplicationRecord> list =
            Assert.IsAssignableFrom<IList<ScriptMappedSensorRequestApplicationRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(record));
    }

    [Fact]
    public void ApplicationResult_GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        var result = new ScriptMappedSensorRequestApplicationResult(
            mapping,
            CreateSingleTankRuntime(),
            new[]
            {
                new ScriptMappedSensorRequestApplicationRecord(
                    0,
                    mapping.GetRecordAtIndex(0),
                    false,
                    null),
            });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void ApplicationResult_GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        var result = new ScriptMappedSensorRequestApplicationResult(
            mapping,
            CreateSingleTankRuntime(),
            new[]
            {
                new ScriptMappedSensorRequestApplicationRecord(
                    0,
                    mapping.GetRecordAtIndex(0),
                    false,
                    null),
            });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    #endregion

    #region Pipeline validation

    [Fact]
    public void Apply_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedSensorRequestApplicationPipeline.Apply(null!, mapping));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Apply_NullMappingResult_ThrowsArgumentNullException_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, null!));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Apply_MappingResultFromDifferentRuntime_ThrowsArgumentException_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtimeA = CreateSingleTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeA,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtimeB, mapping));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    #endregion

    #region Skip behavior

    [Fact]
    public void Apply_None_mapping_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    new ScriptProgram(
                        new[]
                        {
                            new ScriptRoutine(
                                "noop",
                                ScriptCondition.Always(),
                                new ScriptCommand(ScriptCommandType.NoOp, string.Empty)),
                        }),
                });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        MatchSensorRuntimeState runtimeBeforeApply = integration.Runtime;

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.False(applied.Records[0].DidApply);
        Assert.Null(applied.Records[0].ScanResult);
        Assert.Equal(ScriptDomainRequestCategory.None, applied.Records[0].MappingRecord.Category);

        Assert.Same(runtimeBeforeApply, integration.Runtime);
    }

    [Fact]
    public void Apply_Weapon_mapping_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.False(applied.Records[0].DidApply);
        Assert.Equal(ScriptDomainRequestCategory.Weapon, applied.Records[0].MappingRecord.Category);
    }

    [Fact]
    public void Apply_Turret_mapping_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { AimWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.False(applied.Records[0].DidApply);
        Assert.Equal(ScriptDomainRequestCategory.Turret, applied.Records[0].MappingRecord.Category);
    }

    [Fact]
    public void Apply_Movement_mapping_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { RetreatWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.False(applied.Records[0].DidApply);
        Assert.Equal(ScriptDomainRequestCategory.Movement, applied.Records[0].MappingRecord.Category);
    }

    [Fact]
    public void Apply_Sensor_category_with_null_concrete_request_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        MatchScriptDomainRequestMappingResult fullMapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptDomainRequestMappingRecord first = fullMapping.Records[0];
        Assert.Equal(ScriptDomainRequestCategory.Sensor, first.Category);

        ScriptDomainRequestMappingRecord sensorNull =
            ScriptDomainRequestMappingRecord.Sensor(
                first.TankIndex,
                first.TankId,
                first.TranslationOutput,
                sensorRequest: null);

        var remapped = new MatchScriptDomainRequestMappingResult(
            integration,
            new[] { sensorNull });

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, remapped);

        Assert.False(applied.Records[0].DidApply);
        Assert.Null(applied.Records[0].ScanResult);
    }

    [Fact]
    public void Apply_Unsupported_translation_skips_scan()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult fullMapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

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

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, remapped);

        Assert.False(applied.Records[0].DidApply);
    }

    #endregion

    #region Sensor application

    [Fact]
    public void Apply_Sensor_mapping_applies_scan_and_returns_result()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(ScriptDomainRequestCategory.Sensor, mapping.Records[0].Category);
        Assert.True(mapping.Records[0].SensorRequest.HasValue);

        MatchSensorRuntimeState runtimeSnapshotBefore = integration.Runtime;

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.True(applied.Records[0].DidApply);
        Assert.NotNull(applied.Records[0].ScanResult);

        Assert.NotSame(runtimeSnapshotBefore, applied.FinalRuntime);
        Assert.Same(runtimeSnapshotBefore, integration.Runtime);
    }

    [Fact]
    public void Apply_preserves_mapping_result_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.Same(mapping, applied.MappingResult);
    }

    [Fact]
    public void Apply_two_records_processes_sequentially_scan_then_skip()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult applied =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.True(applied.Records[0].DidApply);
        Assert.False(applied.Records[1].DidApply);
        Assert.Equal(0, applied.Records[0].RecordIndex);
        Assert.Equal(1, applied.Records[1].RecordIndex);
    }

    [Fact]
    public void Apply_repeated_calls_same_input_equivalent_outcomes()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult a =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);
        ScriptMappedSensorRequestApplicationResult b =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        Assert.Equal(a.Records[0].DidApply, b.Records[0].DidApply);
        Assert.Equal(
            a.Records[0].ScanResult!.Status,
            b.Records[0].ScanResult!.Status);
        Assert.Equal(
            a.FinalRuntime.State.CurrentTick,
            b.FinalRuntime.State.CurrentTick);
    }

    #endregion
}
