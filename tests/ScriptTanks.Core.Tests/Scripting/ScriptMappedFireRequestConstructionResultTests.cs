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

public sealed class ScriptMappedFireRequestConstructionResultTests
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

    private static MatchScriptDomainRequestMappingResult TwoTankMappingResult()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    FireWhenAlways(),
                    FireWhenAlways(),
                });

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
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

    [Fact]
    public void Constructor_stores_mapping_result_reference()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord record =
            ScriptMappedFireRequestConstructionRecord.NotWeapon(0, mapping.GetRecordAtIndex(0));

        var result = new ScriptMappedFireRequestConstructionResult(
            mapping,
            new[] { record });

        Assert.Same(mapping, result.MappingResult);
    }

    [Fact]
    public void Constructor_rejects_null_mappingResult_ParamName_mappingResult()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestConstructionResult(
                null!,
                new[] { MinimalConstructionRecord(mapping) }));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_records_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedFireRequestConstructionResult(mapping, null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_record_element_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = TwoTankMappingResult();

        ScriptMappedFireRequestConstructionRecord first =
            MinimalConstructionRecord(mapping, recordIndex: 0);

        ScriptMappedFireRequestConstructionRecord?[] bad =
        {
            first,
            null,
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestConstructionResult(mapping, bad!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_count_mismatch_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedFireRequestConstructionResult(
                mapping,
                Array.Empty<ScriptMappedFireRequestConstructionRecord>()));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_defensively_copies_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord first =
            MinimalConstructionRecord(mapping);

        ScriptMappedFireRequestConstructionRecord replacement =
            ScriptMappedFireRequestConstructionRecord.Unsupported(0, mapping.GetRecordAtIndex(0));

        ScriptMappedFireRequestConstructionRecord[] array = { first };

        var result = new ScriptMappedFireRequestConstructionResult(mapping, array);

        array[0] = replacement;

        Assert.Same(first, result.Records[0]);
        Assert.Equal(
            ScriptMappedFireRequestConstructionStatus.NotWeapon,
            result.Records[0].Status);
    }

    [Fact]
    public void Records_collection_is_read_only()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord record =
            MinimalConstructionRecord(mapping);

        var result = new ScriptMappedFireRequestConstructionResult(mapping, new[] { record });

        IList<ScriptMappedFireRequestConstructionRecord> list =
            Assert.IsAssignableFrom<IList<ScriptMappedFireRequestConstructionRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(record));
    }

    [Fact]
    public void Preserves_order_single_record()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord record =
            MinimalConstructionRecord(mapping);

        var result = new ScriptMappedFireRequestConstructionResult(mapping, new[] { record });

        Assert.Equal(0, result.Records[0].RecordIndex);
    }

    [Fact]
    public void GetRecordAtIndex_returns_first_record()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedFireRequestConstructionRecord record =
            MinimalConstructionRecord(mapping);

        var result = new ScriptMappedFireRequestConstructionResult(mapping, new[] { record });

        Assert.Same(record, result.GetRecordAtIndex(0));
    }

    [Fact]
    public void GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        var result = new ScriptMappedFireRequestConstructionResult(
            mapping,
            new[] { MinimalConstructionRecord(mapping) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        var result = new ScriptMappedFireRequestConstructionResult(
            mapping,
            new[] { MinimalConstructionRecord(mapping) });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }
}
