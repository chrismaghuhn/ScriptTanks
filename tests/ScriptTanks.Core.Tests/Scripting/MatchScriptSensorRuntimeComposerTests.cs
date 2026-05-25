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

public sealed class MatchScriptSensorRuntimeComposerTests
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

    #region MatchScriptSensorRuntimeComposerResult

    [Fact]
    public void ComposerResult_Constructor_preserves_references_and_FinalRuntime()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult sensor =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        var result = new MatchScriptSensorRuntimeComposerResult(
            integration,
            mapping,
            sensor);

        Assert.Same(integration, result.IntegrationResult);
        Assert.Same(mapping, result.MappingResult);
        Assert.Same(sensor, result.SensorApplicationResult);
        Assert.Same(sensor.FinalRuntime, result.FinalRuntime);
    }

    [Fact]
    public void ComposerResult_NullIntegrationResult_Throws_ParamName_integrationResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedSensorRequestApplicationResult sensor =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtime, mapping);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptSensorRuntimeComposerResult(
                null!,
                mapping,
                sensor));

        Assert.Equal("integrationResult", ex.ParamName);
    }

    [Fact]
    public void ComposerResult_NullMappingResult_Throws_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        ScriptMappedSensorRequestApplicationResult sensor =
            ScriptMappedSensorRequestApplicationPipeline.Apply(
                runtime,
                ScriptTranslatedCommandDomainMapper.MapAll(integration));

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptSensorRuntimeComposerResult(
                integration,
                null!,
                sensor));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void ComposerResult_NullSensorApplicationResult_Throws_ParamName_sensorApplicationResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptSensorRuntimeComposerResult(
                integration,
                mapping,
                null!));

        Assert.Equal("sensorApplicationResult", ex.ParamName);
    }

    [Fact]
    public void ComposerResult_Mismatched_mapping_integration_throws_ParamName_mappingResult()
    {
        MatchSensorRuntimeState runtimeA = CreateSingleTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integrationA =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeA,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptIntentIntegrationResult integrationB =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeB,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationB);

        ScriptMappedSensorRequestApplicationResult sensorB =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtimeB, mappingB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptSensorRuntimeComposerResult(
                integrationA,
                mappingB,
                sensorB));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void ComposerResult_Mismatched_sensor_mapping_throws_ParamName_sensorApplicationResult()
    {
        MatchSensorRuntimeState runtimeA = CreateSingleTankRuntime();
        MatchSensorRuntimeState runtimeB = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integrationA =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeA,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mappingA =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationA);

        MatchScriptIntentIntegrationResult integrationB =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtimeB,
                new List<ScriptProgram> { FireWhenAlways() });

        MatchScriptDomainRequestMappingResult mappingB =
            ScriptTranslatedCommandDomainMapper.MapAll(integrationB);

        ScriptMappedSensorRequestApplicationResult sensorB =
            ScriptMappedSensorRequestApplicationPipeline.Apply(runtimeB, mappingB);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptSensorRuntimeComposerResult(
                integrationA,
                mappingA,
                sensorB));

        Assert.Equal("sensorApplicationResult", ex.ParamName);
    }

    #endregion

    #region Composer validation

    [Fact]
    public void Run_NullRuntime_ThrowsArgumentNullException_ParamName_runtime()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptSensorRuntimeComposer.Run(
                null!,
                new List<ScriptProgram> { FireWhenAlways() }));

        Assert.Equal("runtime", ex.ParamName);
    }

    [Fact]
    public void Run_NullPrograms_ThrowsArgumentNullException_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            MatchScriptSensorRuntimeComposer.Run(runtime, null!));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_program_count_mismatch_less_than_tanks_delegates_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() }));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_program_count_mismatch_greater_than_tanks_delegates_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                }));

        Assert.Equal("programs", ex.ParamName);
    }

    [Fact]
    public void Run_null_program_element_delegates_ParamName_programs()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram?> { null }!));

        Assert.Equal("programs", ex.ParamName);
    }

    #endregion

    #region Composer behavior

    [Fact]
    public void Run_one_tank_scan_enemy_end_to_end()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        MatchSensorRuntimeState runtimeBefore = runtime;

        MatchScriptSensorRuntimeComposerResult result =
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { ScanWhenAlways() });

        Assert.Equal(1, result.IntegrationResult.Count);
        Assert.Equal(ScriptDomainRequestCategory.Sensor, result.MappingResult.Records[0].Category);
        Assert.True(result.SensorApplicationResult.Records[0].DidApply);
        Assert.NotNull(result.SensorApplicationResult.Records[0].ScanResult);

        Assert.NotSame(runtimeBefore, result.FinalRuntime);
        Assert.Same(runtimeBefore, result.IntegrationResult.Runtime);
    }

    [Fact]
    public void Run_one_tank_fire_maps_weapon_skips_sensor_no_fire_request()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptSensorRuntimeComposerResult result =
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        Assert.Equal(ScriptDomainRequestCategory.Weapon, result.MappingResult.Records[0].Category);
        Assert.False(result.SensorApplicationResult.Records[0].DidApply);
        Assert.Null(result.MappingResult.Records[0].FireRequest);
    }

    [Fact]
    public void Run_one_tank_noop_maps_none_skips_sensor()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptSensorRuntimeComposerResult result =
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { NoOpWhenAlways() });

        Assert.Equal(ScriptDomainRequestCategory.None, result.MappingResult.Records[0].Category);
        Assert.False(result.SensorApplicationResult.Records[0].DidApply);
    }

    [Fact]
    public void Run_two_tanks_preserves_order()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        MatchScriptSensorRuntimeComposerResult result =
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram>
                {
                    ScanWhenAlways(),
                    FireWhenAlways(),
                });

        Assert.Equal(0, result.IntegrationResult.Records[0].TankIndex);
        Assert.Equal(1, result.IntegrationResult.Records[1].TankIndex);

        Assert.Equal(0, result.MappingResult.Records[0].TankIndex);
        Assert.Equal(1, result.MappingResult.Records[1].TankIndex);

        Assert.Equal(0, result.SensorApplicationResult.Records[0].RecordIndex);
        Assert.Equal(1, result.SensorApplicationResult.Records[1].RecordIndex);

        Assert.True(result.SensorApplicationResult.Records[0].DidApply);
        Assert.False(result.SensorApplicationResult.Records[1].DidApply);
    }

    [Fact]
    public void Run_does_not_mutate_original_runtime_reference()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        SimTick tickBefore = runtime.State.CurrentTick;
        TankState tankBefore = runtime.State.Tanks[0];

        _ = MatchScriptSensorRuntimeComposer.Run(
            runtime,
            new List<ScriptProgram> { ScanWhenAlways() });

        Assert.Equal(tickBefore, runtime.State.CurrentTick);
        Assert.Equal(tankBefore, runtime.State.Tanks[0]);
    }

    [Fact]
    public void Run_deterministic_repeated_calls()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();
        var programs = new List<ScriptProgram> { ScanWhenAlways() };

        MatchScriptSensorRuntimeComposerResult a =
            MatchScriptSensorRuntimeComposer.Run(runtime, programs);
        MatchScriptSensorRuntimeComposerResult b =
            MatchScriptSensorRuntimeComposer.Run(runtime, programs);

        Assert.Equal(a.MappingResult.Records[0].Category, b.MappingResult.Records[0].Category);
        Assert.Equal(
            a.SensorApplicationResult.Records[0].DidApply,
            b.SensorApplicationResult.Records[0].DidApply);
        Assert.Equal(
            a.FinalRuntime.State.CurrentTick,
            b.FinalRuntime.State.CurrentTick);
    }

    [Fact]
    public void Run_result_chains_intermediate_references()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptSensorRuntimeComposerResult result =
            MatchScriptSensorRuntimeComposer.Run(
                runtime,
                new List<ScriptProgram> { FireWhenAlways() });

        Assert.Same(result.IntegrationResult, result.MappingResult.IntegrationResult);
        Assert.Same(result.MappingResult, result.SensorApplicationResult.MappingResult);
    }

    #endregion
}
