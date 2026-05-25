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

public sealed class ScriptMappedTurretRequestApplicationResultTests
{
    private static Fixed SampleRotation() => Fixed.FromRatio(1, 4);

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

    private static ScriptProgram AimWhenAlways()
    {
        return new ScriptProgram(
            new[]
            {
                new ScriptRoutine(
                    "aim",
                    ScriptCondition.Always(),
                    new ScriptCommand(ScriptCommandType.AimAtEnemy, argument: "nearest_visible")),
            });
    }

    private static MatchScriptDomainRequestMappingResult MinimalMappingResult()
    {
        MatchSensorRuntimeState runtime = CreateSingleTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram> { AimWhenAlways() });

        return ScriptTranslatedCommandDomainMapper.MapAll(integration);
    }

    private static ScriptMappedTurretRequestApplicationRecord MinimalApplicationRecord(
        MatchScriptDomainRequestMappingResult mapping,
        int recordIndex = 0)
    {
        return ScriptMappedTurretRequestApplicationRecord.SkippedNotTurret(
            recordIndex,
            mapping.GetRecordAtIndex(recordIndex));
    }

    [Fact]
    public void Constructor_preserves_mappingResult_reference()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Same(mapping, result.MappingResult);
    }

    [Fact]
    public void Constructor_preserves_finalRuntime_reference()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Same(finalRuntime, result.FinalRuntime);
    }

    [Fact]
    public void Constructor_count_matches_records_length()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        Assert.Equal(1, result.Count);
        Assert.Equal(result.Count, result.Records.Count);
    }

    [Fact]
    public void Records_collection_is_read_only()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        IList<ScriptMappedTurretRequestApplicationRecord> list =
            Assert.IsAssignableFrom<IList<ScriptMappedTurretRequestApplicationRecord>>(result.Records);

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(applicationRecord));
    }

    [Fact]
    public void Constructor_defensively_copies_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord first =
            MinimalApplicationRecord(mapping);

        ScriptMappedTurretRequestApplicationRecord replacement =
            ScriptMappedTurretRequestApplicationRecord.Applied(
                99,
                mapping.GetRecordAtIndex(0),
                SampleRotation());

        ScriptMappedTurretRequestApplicationRecord[] array = { first };

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            array);

        array[0] = replacement;

        Assert.Same(first, result.Records[0]);
        Assert.Equal(0, result.Records[0].RecordIndex);
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

    [Fact]
    public void Preserves_order_two_records()
    {
        MatchSensorRuntimeState runtime = CreateTwoTankRuntime();

        MatchScriptIntentIntegrationResult integration =
            MatchScriptIntentIntegrationComposer.EvaluateIntents(
                runtime,
                new List<ScriptProgram>
                {
                    AimWhenAlways(),
                    AimWhenAlways(),
                });

        MatchScriptDomainRequestMappingResult mapping =
            ScriptTranslatedCommandDomainMapper.MapAll(integration);

        ScriptMappedTurretRequestApplicationRecord record0 =
            ScriptMappedTurretRequestApplicationRecord.Applied(
                0,
                mapping.GetRecordAtIndex(0),
                SampleRotation());

        ScriptMappedTurretRequestApplicationRecord record1 =
            ScriptMappedTurretRequestApplicationRecord.NoTarget(
                1,
                mapping.GetRecordAtIndex(1));

        Assert.Equal(ScriptDomainRequestCategory.Turret, mapping.GetRecordAtIndex(0).Category);
        Assert.Equal(ScriptDomainRequestCategory.Turret, mapping.GetRecordAtIndex(1).Category);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            runtime,
            new[] { record0, record1 });

        Assert.Equal(2, result.Count);
        Assert.Same(record0, result.GetRecordAtIndex(0));
        Assert.Same(record1, result.GetRecordAtIndex(1));
    }

    [Fact]
    public void Constructor_rejects_null_mappingResult_ParamName_mappingResult()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedTurretRequestApplicationResult(
                null!,
                finalRuntime,
                new[] { applicationRecord }));

        Assert.Equal("mappingResult", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_finalRuntime_ParamName_finalRuntime()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedTurretRequestApplicationResult(
                mapping,
                null!,
                new[] { applicationRecord }));

        Assert.Equal("finalRuntime", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_records_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new ScriptMappedTurretRequestApplicationResult(
                mapping,
                finalRuntime,
                null!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_null_record_element_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord?[] bad = { null };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedTurretRequestApplicationResult(
                mapping,
                finalRuntime,
                bad!));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_count_mismatch_ParamName_records()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            new ScriptMappedTurretRequestApplicationResult(
                mapping,
                finalRuntime,
                Array.Empty<ScriptMappedTurretRequestApplicationRecord>()));

        Assert.Equal("records", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_negative_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(-1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void GetRecordAtIndex_rejects_past_end_ParamName_recordIndex()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var result = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetRecordAtIndex(1));

        Assert.Equal("recordIndex", ex.ParamName);
    }

    [Fact]
    public void Result_does_not_override_equality()
    {
        MatchScriptDomainRequestMappingResult mapping = MinimalMappingResult();
        MatchSensorRuntimeState finalRuntime = CreateSingleTankRuntime();

        ScriptMappedTurretRequestApplicationRecord applicationRecord =
            MinimalApplicationRecord(mapping);

        var a = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        var b = new ScriptMappedTurretRequestApplicationResult(
            mapping,
            finalRuntime,
            new[] { applicationRecord });

        Assert.False(ReferenceEquals(a, b));
        Assert.NotSame(a, b);
    }
}
