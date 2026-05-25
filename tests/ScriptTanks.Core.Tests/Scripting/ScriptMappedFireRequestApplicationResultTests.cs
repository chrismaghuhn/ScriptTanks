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

public sealed class ScriptMappedFireRequestApplicationResultTests
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

    private static MatchScriptDomainRequestMappingResult MinimalMappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
    }

    private static ScriptMappedFireRequestConstructionRecord MinimalConstructionRecord(
        MatchScriptDomainRequestMappingResult mapping,
        int recordIndex = 0)
    {
        return ScriptMappedFireRequestConstructionRecord.NotWeapon(
            recordIndex,
            mapping.GetRecordAtIndex(recordIndex));
    }

    private static ScriptMappedFireRequestConstructionPipelineResult MinimalConstructionPipelineResult()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord constructionRecord =
            MinimalConstructionRecord(mapping);

        var constructionResult = new ScriptMappedFireRequestConstructionResult(
            mapping,
            new[] { constructionRecord });

        return new ScriptMappedFireRequestConstructionPipelineResult(
            constructionResult,
            new ProjectileIdSequence(0));
    }

    private static ScriptMappedFireRequestApplicationRecord MinimalApplicationRecord(
        ScriptMappedFireRequestConstructionRecord constructionRecord,
        int recordIndex = 0)
    {
        return ScriptMappedFireRequestApplicationRecord.SkippedNotConstructed(
            recordIndex,
            constructionRecord);
    }

    [Fact]
    public void Constructor_stores_construction_pipeline_result_reference()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestConstructionRecord constructionRecord =
            pipelineResult.ConstructionResult.GetRecordAtIndex(0);

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(constructionRecord);

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Same(pipelineResult, result.ConstructionPipelineResult);
    }

    [Fact]
    public void Constructor_stores_final_runtime_reference()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Same(finalRuntime, result.FinalRuntime);
    }

    [Fact]
    public void Constructor_rejects_null_constructionPipelineResult_ParamName_constructionPipelineResult()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestApplicationResult(
                null!,
                finalRuntime,
                new[] { applicationRecord }));

        Assert.Equal("constructionPipelineResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_finalRuntime_ParamName_finalRuntime()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestApplicationResult(
                pipelineResult,
                null!,
                new[] { applicationRecord }));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_records_ParamName_records()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestApplicationResult(
                pipelineResult,
                finalRuntime,
                null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_record_element_ParamName_records()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedFireRequestApplicationRecord?[] bad = { null };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationResult(
                pipelineResult,
                finalRuntime,
                bad!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_count_mismatch_ParamName_records()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestApplicationResult(
                pipelineResult,
                finalRuntime,
                Array.Empty<ScriptMappedFireRequestApplicationRecord>()));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_defensively_copies_records()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestConstructionRecord constructionRecord =
            pipelineResult.ConstructionResult.GetRecordAtIndex(0);

        ScriptMappedFireRequestApplicationRecord first =
            MinimalApplicationRecord(constructionRecord);

        ScriptMappedFireRequestApplicationRecord replacement =
            ScriptMappedFireRequestApplicationRecord.SkippedNotConstructed(
                99,
                constructionRecord);

        ScriptMappedFireRequestApplicationRecord[] array = { first };

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            array);

        array[0] = replacement;

        Assert.Same(first, result.Records[0]);
        Assert.Equal(0, result.Records[0].RecordIndex);
    }

    [Fact]
    public void Records_collection_is_read_only()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        IList<ScriptMappedFireRequestApplicationRecord> list =
            Assert.IsAssignableFrom<IList<ScriptMappedFireRequestApplicationRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(applicationRecord));
    }

    [Fact]
    public void Preserves_order_single_record()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(
                pipelineResult.ConstructionResult.GetRecordAtIndex(0),
                recordIndex: 0);

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Equal(0, result.Records[0].RecordIndex);
    }

    [Fact]
    public void GetRecordAtIndex_returns_first_record()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Same(applicationRecord, result.GetRecordAtIndex(0));
    }

    [Fact]
    public void GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        ScriptMappedFireRequestConstructionPipelineResult pipelineResult =
            MinimalConstructionPipelineResult();

        ScriptMappedFireRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(pipelineResult.ConstructionResult.GetRecordAtIndex(0));

        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        var result = new ScriptMappedFireRequestApplicationResult(
            pipelineResult,
            finalRuntime,
            new[] { applicationRecord });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }
}
