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

public sealed class MatchScriptIntentIntegrationComposerTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null,
        int? hp = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                position ?? FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
            hp ?? TankCatalog.BasicTank.Stats.MaxHitPoints,
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

    private static ScriptCommand Command(
        ScriptCommandType type,
        string argument = "")
    {
        return new ScriptCommand(type, argument);
    }

    private static ScriptCondition Always()
    {
        return ScriptCondition.Always();
    }

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand command)
    {
        return new ScriptRoutine(
            name,
            condition,
            command);
    }

    private static ScriptProgram CreateProgram(params ScriptRoutine[] routines)
    {
        return new ScriptProgram(routines);
    }

    private static ScriptProgram FireWhenAlways()
    {
        return CreateProgram(
            CreateRoutine(
                "fire",
                Always(),
                Command(ScriptCommandType.Fire)));
    }

    #region MatchScriptIntentIntegrationRecord

    [Fact]
    public void IntegrationRecord_Constructor_rejects_negative_tankIndex_ParamName_tankIndex()
    {
        ScriptRuntimeEvaluationRecord evaluationRecord =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(10),
                tankIndex: 0,
                FireWhenAlways(),
                new ScriptEvaluationContext(
                    enemyVisible: false,
                    weaponReady: true,
                    myHitPoints: 50,
                    enemyDistance: Fixed.FromInt(10),
                    sensorReady: true,
                    turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus));

        ScriptCommandTranslationOutput translationOutput =
            ScriptCommandTranslatorV2.Translate(evaluationRecord.Result.Intent);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MatchScriptIntentIntegrationRecord(
                tankIndex: -1,
                new TankId(0),
                evaluationRecord.Context,
                evaluationRecord,
                translationOutput));

        Assert.Equal("tankIndex", ex.ParamName);
    }

    [Fact]
    public void IntegrationRecord_Constructor_rejects_null_evaluationRecord_ParamName_evaluationRecord()
    {
        ScriptCommandTranslationOutput translationOutput =
            ScriptCommandTranslatorV2.Translate(ScriptCommandIntent.None());

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptIntentIntegrationRecord(
                0,
                new TankId(0),
                new ScriptEvaluationContext(
                    false,
                    false,
                    0,
                    Fixed.Zero,
                    false,
                    ScriptingTestFixtures.DefaultNoTargetAimStatus),
                evaluationRecord: null!,
                translationOutput));

        Assert.Equal("evaluationRecord", ex.ParamName);
    }

    [Fact]
    public void IntegrationRecord_Constructor_rejects_null_translationOutput_ParamName_translationOutput()
    {
        ScriptRuntimeEvaluationRecord evaluationRecord =
            ScriptRuntimeEvaluationPipeline.Evaluate(
                new SimTick(10),
                0,
                FireWhenAlways(),
                new ScriptEvaluationContext(
                    false,
                    true,
                    50,
                    Fixed.FromInt(10),
                    true,
                    ScriptingTestFixtures.DefaultNoTargetAimStatus));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptIntentIntegrationRecord(
                0,
                new TankId(0),
                evaluationRecord.Context,
                evaluationRecord,
                translationOutput: null!));

        Assert.Equal("translationOutput", ex.ParamName);
    }

    #endregion

    #region MatchScriptIntentIntegrationResult

    [Fact]
    public void IntegrationResult_Constructor_rejects_null_runtime_ParamName_runtime()
    {
        MatchScriptIntentIntegrationRecord record = CreateMinimalIntegrationRecord();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptIntentIntegrationResult(runtime: null!, new[] { record }));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void IntegrationResult_Constructor_rejects_null_records_ParamName_records()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptIntentIntegrationResult(runtime, records: null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void IntegrationResult_Constructor_rejects_null_record_element_ParamName_records()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptIntentIntegrationRecord record = CreateMinimalIntegrationRecord();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptIntentIntegrationResult(
                runtime,
                new MatchScriptIntentIntegrationRecord?[] { record, null }!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void IntegrationResult_Constructor_defensively_copies_records_array()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptIntentIntegrationRecord first = CreateMinimalIntegrationRecord();
        MatchScriptIntentIntegrationRecord second = CreateMinimalIntegrationRecord();

        MatchScriptIntentIntegrationRecord[] array = { first, second };
        var result = new MatchScriptIntentIntegrationResult(runtime, array);

        array[1] = first;

        Assert.Same(second, result.Records[1]);
    }

    [Fact]
    public void IntegrationResult_Records_is_read_only()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchScriptIntentIntegrationRecord record = CreateMinimalIntegrationRecord();

        var result = new MatchScriptIntentIntegrationResult(runtime, new[] { record });

        IList<MatchScriptIntentIntegrationRecord> list =
            Assert.IsAssignableFrom<IList<MatchScriptIntentIntegrationRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(record));
    }

    [Fact]
    public void IntegrationResult_GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        var result = new MatchScriptIntentIntegrationResult(
            runtime,
            new[] { CreateMinimalIntegrationRecord() });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void IntegrationResult_GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        var result = new MatchScriptIntentIntegrationResult(
            runtime,
            new[] { CreateMinimalIntegrationRecord() });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    #endregion

    #region Composer validation

    [Fact]
    public void EvaluateIntents_rejects_null_runtime_ParamName_runtime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                null!,
                new List<ScriptProgram> { FireWhenAlways() }));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void EvaluateIntents_rejects_null_programs_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, null!));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void EvaluateIntents_rejects_program_count_less_than_tank_count_ParamName_programs()
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
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() }));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void EvaluateIntents_rejects_program_count_greater_than_tank_count_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                }));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void EvaluateIntents_rejects_null_program_element_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram?> { null }!));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Composer behavior

    [Fact]
    public void EvaluateIntents_one_tank_returns_one_record()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        Assert.Equal(1, result.Count);
        Assert.Single(result.Records);
    }

    [Fact]
    public void EvaluateIntents_two_tanks_returns_records_in_index_order()
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
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                });

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result.Records[0].TankIndex);
        Assert.Equal(1, result.Records[1].TankIndex);
    }

    [Fact]
    public void EvaluateIntents_record_has_correct_tankIndex_and_tankId()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(7, 0),
            CreateTank(9, 1));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                });

        Assert.Equal(0, result.Records[0].TankIndex);
        Assert.Equal(new TankId(7), result.Records[0].TankId);
        Assert.Equal(1, result.Records[1].TankIndex);
        Assert.Equal(new TankId(9), result.Records[1].TankId);
    }

    [Fact]
    public void EvaluateIntents_context_matches_scriptRuntimeContextBuilder_for_hp()
    {
        int hp = 42;
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, hp: hp));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptEvaluationContext expected =
            ScriptRuntimeContextBuilder.Build(runtime, 0);

        Assert.Equal(expected, result.Records[0].Context);
        Assert.Equal(hp, result.Records[0].Context.MyHitPoints);
    }

    [Fact]
    public void EvaluateIntents_translation_output_uses_v2_for_fire_command()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptCommandTranslationOutput output = result.Records[0].TranslationOutput;

        Assert.Equal(
            ScriptTranslatedCommandRequestKind.Fire,
            output.Request.Kind);

        Assert.Equal("default", output.Request.Payload);
        Assert.Equal(
            ScriptCommandTranslationStatus.Translated,
            output.Result.Status);
    }

    [Fact]
    public void EvaluateIntents_destroyed_tank_record_reflects_destroyed_context_from_builder()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, hp: 0));
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptEvaluationContext ctx = result.Records[0].Context;

        Assert.Equal(0, ctx.MyHitPoints);
        Assert.False(ctx.WeaponReady);
        Assert.False(ctx.SensorReady);
        Assert.False(ctx.EnemyVisible);
        Assert.Equal(Fixed.Zero, ctx.EnemyDistance);
    }

    [Fact]
    public void EvaluateIntents_result_preserves_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult result =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        Assert.Same(runtime, result.Runtime);
    }

    [Fact]
    public void EvaluateIntents_does_not_mutate_runtime_state()
    {
        SimTick tick = new SimTick(10);
        TankState tank0 = CreateTank(0, 0, FixedVec2.FromInts(0, 0));
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            tank0);
        MatchSensorRuntimeState runtime = CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));

        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tankBefore = runtime.State.Tanks[0];

        _ = MatchScriptIntentIntegrationComposer.EvaluateIntents(
            runtime,
            new List<ScriptProgram> { FireWhenAlways() });

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tankBefore, runtime.State.Tanks[0]);
    }

    [Fact]
    public void EvaluateIntents_repeated_calls_produce_equivalent_outputs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        var programs = new List<ScriptProgram> { FireWhenAlways() };

        MatchScriptIntentIntegrationResult a =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);
        MatchScriptIntentIntegrationResult b =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(runtime, programs);

        Assert.Equal(a.Records[0].Context, b.Records[0].Context);
        Assert.Equal(
            a.Records[0].TranslationOutput.Request.Kind,
            b.Records[0].TranslationOutput.Request.Kind);
        Assert.Equal(
            a.Records[0].TranslationOutput.Request.Payload,
            b.Records[0].TranslationOutput.Request.Payload);
        Assert.Equal(a.Count, b.Count);
    }

    #endregion

    private static MatchSensorRuntimeState CreateSingleTankRuntime()
    {
        MatchState state = CreateMatchState(
            new SimTick(10),
            new[]
            {
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static MatchScriptIntentIntegrationRecord CreateMinimalIntegrationRecord()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        return MatchScriptIntentIntegrationComposer.EvaluateIntents(
            runtime,
            new List<ScriptProgram> { FireWhenAlways() }).Records[0];
    }
}
