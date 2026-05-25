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

public sealed class MatchScriptDomainRequestMappingResultTests
{
    private static TankState CreateTank(
        int id,
        int ownerSlot,
        FixedVec2? position = null)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                position ?? FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
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

    private static ScriptRoutine CreateRoutine(
        string name,
        ScriptCondition condition,
        ScriptCommand command)
    {
        return new ScriptRoutine(name, condition, command);
    }

    private static ScriptProgram FireWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                CreateRoutine(
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
            CreateTank(0, 0, FixedVec2.FromInts(0, 0)));
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

    private static ScriptDomainRequestMappingRecord MappingForIntegrationRecord(
        MatchScriptIntentIntegrationRecord integrationRecord,
        ScriptDomainRequestCategory category)
    {
        return new ScriptDomainRequestMappingRecord(
            integrationRecord.TankIndex,
            integrationRecord.TankId,
            integrationRecord.TranslationOutput,
            category,
            sensorRequest: null,
            fireRequest: null);
    }

    [Fact]
    public void Constructor_preserves_integrationResult_reference()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        var result = new MatchScriptDomainRequestMappingResult(
            integration,
            new[] { mapping });

        Assert.Same(integration, result.IntegrationResult);
    }

    [Fact]
    public void Constructor_rejects_null_integrationResult_ParamName_integrationResult()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptDomainRequestMappingResult(null!, new[] { mapping }));

        Assert.Equal("integrationResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_records_ParamName_records()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new MatchScriptDomainRequestMappingResult(integration, null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_record_element_ParamName_records()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        MatchScriptIntentIntegrationRecord ir = integration.Records[0];

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(ir, ScriptDomainRequestCategory.None);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptDomainRequestMappingResult(
                integration,
                new ScriptDomainRequestMappingRecord?[] { mapping, null }!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_defensively_copies_records_array()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        MatchScriptIntentIntegrationRecord ir = integration.Records[0];

        ScriptDomainRequestMappingRecord first =
            MappingForIntegrationRecord(ir, ScriptDomainRequestCategory.None);

        ScriptDomainRequestMappingRecord replacement =
            MappingForIntegrationRecord(ir, ScriptDomainRequestCategory.Unsupported);

        ScriptDomainRequestMappingRecord[] array = { first };

        var result = new MatchScriptDomainRequestMappingResult(integration, array);

        array[0] = replacement;

        Assert.Same(first, result.Records[0]);
        Assert.Equal(ScriptDomainRequestCategory.None, result.Records[0].Category);
    }

    [Fact]
    public void Records_collection_is_read_only()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        var result = new MatchScriptDomainRequestMappingResult(integration, new[] { mapping });

        IList<ScriptDomainRequestMappingRecord> list =
            Assert.IsAssignableFrom<IList<ScriptDomainRequestMappingRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(mapping));
    }

    [Fact]
    public void Preserves_input_order_for_two_tanks()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        ScriptDomainRequestMappingRecord first = MappingForIntegrationRecord(
            integration.Records[0],
            ScriptDomainRequestCategory.Sensor);

        ScriptDomainRequestMappingRecord second = MappingForIntegrationRecord(
            integration.Records[1],
            ScriptDomainRequestCategory.Weapon);

        var result = new MatchScriptDomainRequestMappingResult(
            integration,
            new[] { first, second });

        Assert.Equal(ScriptDomainRequestCategory.Sensor, result.Records[0].Category);
        Assert.Equal(ScriptDomainRequestCategory.Weapon, result.Records[1].Category);
        Assert.Equal(0, result.Records[0].TankIndex);
        Assert.Equal(1, result.Records[1].TankIndex);
    }

    [Fact]
    public void Count_returns_record_count()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        ScriptDomainRequestMappingRecord first = MappingForIntegrationRecord(
            integration.Records[0],
            ScriptDomainRequestCategory.None);

        ScriptDomainRequestMappingRecord second = MappingForIntegrationRecord(
            integration.Records[1],
            ScriptDomainRequestCategory.None);

        var result = new MatchScriptDomainRequestMappingResult(
            integration,
            new[] { first, second });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetRecordAtIndex_zero_returns_first_record()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.Turret);

        var result = new MatchScriptDomainRequestMappingResult(integration, new[] { mapping });

        Assert.Same(mapping, result.GetRecordAtIndex(0));
    }

    [Fact]
    public void GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        var result = new MatchScriptDomainRequestMappingResult(integration, new[] { mapping });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        MatchScriptIntentIntegrationResult integration = CreateSingleTankIntegrationResult();

        ScriptDomainRequestMappingRecord mapping =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        var result = new MatchScriptDomainRequestMappingResult(integration, new[] { mapping });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_record_count_mismatch_ParamName_records()
    {
        MatchScriptIntentIntegrationResult integration = CreateTwoTankIntegrationResult();

        ScriptDomainRequestMappingRecord only =
            MappingForIntegrationRecord(integration.Records[0], ScriptDomainRequestCategory.None);

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new MatchScriptDomainRequestMappingResult(integration, new[] { only }));

        Assert.Equal("records", ex.ParamName);
    }
}
