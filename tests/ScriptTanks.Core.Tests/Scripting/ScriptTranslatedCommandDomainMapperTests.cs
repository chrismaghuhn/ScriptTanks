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

public sealed class ScriptTranslatedCommandDomainMapperTests
{
    private static ScriptEvaluationContext DefaultContext()
    {
        return new ScriptEvaluationContext(
            enemyVisible: false,
            weaponReady: true,
            myHitPoints: 50,
            enemyDistance: Fixed.FromInt(10),
            sensorReady: true,
            turretAimStatus: ScriptingTestFixtures.DefaultNoTargetAimStatus);
    }

    private static ScriptProgram MinimalProgram(ScriptCommandType commandType)
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "r",
                    ScriptCondition.Always(),
                    new ScriptCommand(commandType, argument: string.Empty)),
            });
    }

    private static ScriptRuntimeEvaluationRecord MinimalEvalRecord(
        int tankIndex,
        ScriptProgram program)
    {
        return ScriptRuntimeEvaluationPipeline.Evaluate(
            new SimTick(1),
            tankIndex,
            program,
            DefaultContext());
    }

    private static MatchScriptIntentIntegrationRecord CreateRecord(
        int tankIndex,
        TankId tankId,
        ScriptCommandTranslationOutput translationOutput,
        ScriptCommandType programCommandType = ScriptCommandType.NoOp)
    {
        ScriptRuntimeEvaluationRecord eval =
            MinimalEvalRecord(tankIndex, MinimalProgram(programCommandType));

        return new MatchScriptIntentIntegrationRecord(
            tankIndex,
            tankId,
            eval.Context,
            eval,
            translationOutput);
    }

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

    private static MatchScriptIntentIntegrationResult CreateSingleTankIntegrationResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        return MatchScriptIntentIntegrationComposer.EvaluateIntents(
            runtime,
            new List<ScriptProgram> { FireWhenAlways() });
    }

    private static MatchScriptIntentIntegrationResult CreateTwoTankIntegrationResult()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        return MatchScriptIntentIntegrationComposer.EvaluateIntents(
            runtime,
            new List<ScriptProgram>
            {
                FireWhenAlways(),
                FireWhenAlways(),
            });
    }

    #region Validation

    [Fact]
    public void Map_NullRecord_ThrowsArgumentNullException_ParamName_record()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptTranslatedCommandDomainMapper.Map(null!));

        Assert.Equal("record", ex.ParamName);
    }

    [Fact]
    public void MapAll_NullIntegrationResult_ThrowsArgumentNullException_ParamName_integrationResult()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            ScriptTranslatedCommandDomainMapper.MapAll(null!));

        Assert.Equal("integrationResult", ex.ParamName);
    }

    #endregion

    #region Per-status and per-kind mapping

    [Fact]
    public void Map_NoIntent_maps_to_None()
    {
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.NoIntent(),
            ScriptTranslatedCommandRequest.None());

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(3, new TankId(9), output);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.None, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_Translated_NoOp_maps_to_None()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.NoOp, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.NoOp,
                routineIndex: 0,
                cmd,
                payload: string.Empty));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.None, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_Translated_ScanEnemy_maps_to_Sensor_with_concrete_scan_request()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.ScanEnemy, "default");
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.ScanEnemy,
                routineIndex: 0,
                cmd,
                payload: "default"));

        int tankIndex = 4;
        MatchScriptIntentIntegrationRecord record =
            CreateRecord(tankIndex, new TankId(2), output, ScriptCommandType.ScanEnemy);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Sensor, mapped.Category);
        Assert.True(mapped.HasConcreteRequest);
        Assert.NotNull(mapped.SensorRequest);

        MatchSensorScanRequest expected =
            new MatchSensorScanRequest(tankIndex, SensorSlot.Zero);

        Assert.Equal(expected, mapped.SensorRequest!.Value);
        Assert.Null(mapped.FireRequest);
    }

    [Fact]
    public void Map_Translated_AimAtEnemy_maps_to_Turret_placeholder()
    {
        ScriptCommand cmd =
            new ScriptCommand(ScriptCommandType.AimAtEnemy, "nearest_visible");
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.AimAtEnemy,
                routineIndex: 0,
                cmd,
                payload: "nearest_visible"));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output, ScriptCommandType.AimAtEnemy);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Turret, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_Translated_Fire_maps_to_Weapon_without_MatchFireRequest()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Fire, "default");
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Fire,
                routineIndex: 0,
                cmd,
                payload: "default"));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output, ScriptCommandType.Fire);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Weapon, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
        Assert.Null(mapped.FireRequest);
        Assert.Null(mapped.SensorRequest);
    }

    [Fact]
    public void Map_Translated_MoveToPatrolPoint_maps_to_Movement_placeholder()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.MoveToPatrolPoint,
                routineIndex: 0,
                cmd,
                payload: "next"));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output, ScriptCommandType.MoveToPatrolPoint);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Movement, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_Translated_Retreat_maps_to_Movement_placeholder()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Retreat, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.Retreat,
                routineIndex: 0,
                cmd,
                payload: "away"));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output, ScriptCommandType.Retreat);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Movement, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_UnsupportedCommand_maps_to_Unsupported()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Fire, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.UnsupportedCommand(0, cmd, "unsupported"),
            ScriptTranslatedCommandRequest.None());

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Unsupported, mapped.Category);
        Assert.False(mapped.HasConcreteRequest);
    }

    [Fact]
    public void Map_InvalidCommandArgument_maps_to_Unsupported()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Fire, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.InvalidCommandArgument(0, cmd, "bad arg"),
            ScriptTranslatedCommandRequest.None());

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Unsupported, mapped.Category);
    }

    [Fact]
    public void Map_MissingHardware_maps_to_Unsupported()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.Fire, string.Empty);
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.MissingHardware(0, cmd, "no gun"),
            ScriptTranslatedCommandRequest.None());

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(0, new TankId(1), output);

        ScriptDomainRequestMappingRecord mapped =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(ScriptDomainRequestCategory.Unsupported, mapped.Category);
    }

    #endregion

    #region Batch mapping

    [Fact]
    public void MapAll_ReturnsSameIntegrationResultReference()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        MatchScriptDomainRequestMappingResult result =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Same(integration, result.IntegrationResult);
    }

    [Fact]
    public void MapAll_PreservesRecordCount()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        MatchScriptDomainRequestMappingResult result =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(integration.Count, result.Count);
        Assert.Equal(integration.Count, result.Records.Count);
    }

    [Fact]
    public void MapAll_PreservesRecordOrder_and_maps_each()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        MatchScriptDomainRequestMappingResult result =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(0, result.Records[0].TankIndex);
        Assert.Equal(1, result.Records[1].TankIndex);

        Assert.Equal(
            integration.Records[0].TranslationOutput.Request.Kind,
            result.Records[0].TranslationOutput.Request.Kind);

        Assert.Equal(
            integration.Records[1].TranslationOutput.Request.Kind,
            result.Records[1].TranslationOutput.Request.Kind);
    }

    [Fact]
    public void MapAll_OutputSatisfiesResultCountInvariant()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        MatchScriptDomainRequestMappingResult result =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(integration.Count, result.Count);
        Assert.Equal(result.Count, result.Records.Count);
    }

    #endregion

    #region Purity and determinism

    [Fact]
    public void MapAll_does_not_mutate_runtime_state()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        SimTick tickBefore = integration.Runtime.State.CurrentTick;
        TankState tankBefore = integration.Runtime.State.Tanks[0];

        _ = ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(tickBefore, integration.Runtime.State.CurrentTick);
        Assert.Equal(tankBefore, integration.Runtime.State.Tanks[0]);
    }

    [Fact]
    public void Map_deterministic_for_same_record()
    {
        ScriptCommand cmd = new ScriptCommand(ScriptCommandType.ScanEnemy, "default");
        ScriptCommandTranslationOutput output = new ScriptCommandTranslationOutput(
            ScriptCommandTranslationResult.Translated(0, cmd, "ok"),
            ScriptTranslatedCommandRequest.Create(
                ScriptTranslatedCommandRequestKind.ScanEnemy,
                routineIndex: 0,
                cmd,
                payload: "default"));

        MatchScriptIntentIntegrationRecord record =
            CreateRecord(2, new TankId(7), output, ScriptCommandType.ScanEnemy);

        ScriptDomainRequestMappingRecord a =
            ScriptTranslatedCommandDomainMapper.Map(record);
        ScriptDomainRequestMappingRecord b =
            ScriptTranslatedCommandDomainMapper.Map(record);

        Assert.Equal(a.Category, b.Category);
        Assert.Equal(a.HasConcreteRequest, b.HasConcreteRequest);
        Assert.Equal(a.SensorRequest, b.SensorRequest);
    }

    [Fact]
    public void MapAll_deterministic_for_same_integration()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        MatchScriptDomainRequestMappingResult a =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);
        MatchScriptDomainRequestMappingResult b =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a.Records[i].Category, b.Records[i].Category);
            Assert.Equal(a.Records[i].HasConcreteRequest, b.Records[i].HasConcreteRequest);
        }
    }

    #endregion
}
