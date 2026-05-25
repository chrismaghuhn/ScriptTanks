using System;
using System.Collections.Generic;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
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

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombinedRuntimeTickCombatLogFactoryTests
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
            new MovementState(position ?? FixedVec2.FromInts(10 + id, 20), FixedVec2.Zero),
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

    private static MatchSensorRuntimeState CreateTwoTankRuntime(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
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

    private static MatchSensorRuntimeState CreateTwoTankRuntimeFirstFired(SimTick tick)
    {
        MatchState state = CreateMatchState(
            tick,
            new[]
            {
                CreateFiredLoadout(tick),
                CreateWeaponLoadout(WeaponState.Ready(WeaponCatalog.StandardCannon)),
            },
            CreateTank(0, 0),
            CreateTank(1, 1));
        return CreateRuntime(
            state,
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)),
            CreateSensorLoadout(SensorState.Ready(SensorCatalog.BasicRadar)));
    }

    private static CombinedRuntimeTickResult CreateScanOnlyTickResult()
    {
        return CombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(0)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                ScanWhenAlways(),
            },
            new ProjectileIdSequence(0));
    }

    private static CombinedRuntimeTickResult CreateAppliedFireTickResult()
    {
        return CombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntime(new SimTick(10)),
            new List<ScriptProgram>
            {
                ScanWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(42));
    }

    private static CombinedRuntimeTickResult CreateMixedRejectedThenAppliedTickResult()
    {
        return CombinedRuntimeTickPipeline.Step(
            CreateTwoTankRuntimeFirstFired(new SimTick(10)),
            new List<ScriptProgram>
            {
                FireWhenAlways(),
                FireWhenAlways(),
            },
            new ProjectileIdSequence(0));
    }

    private static string ExpectedScriptTickMessage(int tick, int tanks, int constructed, int applied, int rejected)
    {
        return FormattableString.Invariant(
            $"Script tick: tick={tick} tanks={tanks} fires_constructed={constructed} fires_applied={applied} fires_rejected={rejected}.");
    }

    private static string ExpectedAppliedMessage(int tick, int recordIndex, int tankId, int projectileId, int weaponSlot)
    {
        return FormattableString.Invariant(
            $"Script fire applied: tick={tick} record_index={recordIndex} tank_id={tankId} projectile_id={projectileId} weapon_slot={weaponSlot}.");
    }

    private static string ExpectedRejectedMessage(int tick, int recordIndex, int tankId)
    {
        return FormattableString.Invariant(
            $"Script fire rejected: tick={tick} record_index={recordIndex} tank_id={tankId} reason=not_ready.");
    }

    #region Validation

    [Fact]
    public void CreateTickLog_NullTickResult_ThrowsArgumentNullException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            CombinedRuntimeTickCombatLogFactory.CreateTickLog(null!));

        Assert.Equal("tickResult", ex.ParamName);
    }

    #endregion

    #region script_tick

    [Fact]
    public void CreateTickLog_AlwaysEmitsScriptTickFirst()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateAppliedFireTickResult());

        Assert.Equal(CombatLogEventTypes.ScriptTick, log.Entries[0].EventType);
    }

    [Fact]
    public void CreateTickLog_ScriptTick_UsesPreStepTick()
    {
        CombinedRuntimeTickResult tickResult = CreateAppliedFireTickResult();
        SimTick preStepTick = tickResult.InitialRuntime.State.CurrentTick;

        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        Assert.All(log.Entries, entry => Assert.Equal(preStepTick, entry.Tick));
        Assert.Equal(new SimTick(10), preStepTick);
    }

    [Fact]
    public void CreateTickLog_ScriptTick_MessageMatchesExpected()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateScanOnlyTickResult());

        Assert.Equal(
            ExpectedScriptTickMessage(tick: 0, tanks: 2, constructed: 0, applied: 0, rejected: 0),
            log.Entries[0].Message);
    }

    [Fact]
    public void CreateTickLog_ScriptTick_MessageContainsCounts()
    {
        CombinedRuntimeTickResult tickResult = CreateAppliedFireTickResult();
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        Assert.Contains("fires_constructed=1", log.Entries[0].Message);
        Assert.Contains("fires_applied=1", log.Entries[0].Message);
        Assert.Contains("fires_rejected=0", log.Entries[0].Message);
    }

    [Fact]
    public void CreateTickLog_ScanOnlyTick_EmitsExactlyOneScriptTickEntry()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateScanOnlyTickResult());

        Assert.Single(log.Entries);
        Assert.Equal(CombatLogEventTypes.ScriptTick, log.Entries[0].EventType);
        Assert.Equal(CombatLogCategory.Match, log.Entries[0].Category);
    }

    #endregion

    #region Applied fire

    [Fact]
    public void CreateTickLog_FireTick_EmitsScriptTickThenFireRequestApplied()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateAppliedFireTickResult());

        Assert.Equal(2, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.ScriptTick, log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, log.Entries[1].EventType);
    }

    [Fact]
    public void CreateTickLog_AppliedFire_MessageMatchesExpected()
    {
        CombinedRuntimeTickResult tickResult = CreateAppliedFireTickResult();
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        ScriptMappedFireRequestApplicationRecord appliedRecord =
            tickResult.ScriptResult.FireApplicationResult.Records[1];

        Assert.Equal(
            ExpectedAppliedMessage(
                tick: 10,
                recordIndex: appliedRecord.RecordIndex,
                tankId: appliedRecord.ConstructionRecord.FireRequest!.Value.ShooterTankId.Value,
                projectileId: appliedRecord.FireOutcome!.SpawnedProjectile!.Value.Id.Value,
                weaponSlot: appliedRecord.ConstructionRecord.FireRequest!.Value.WeaponSlot.Value),
            log.Entries[1].Message);
    }

    [Fact]
    public void CreateTickLog_AppliedFire_MessageContainsRecordIndexTankIdProjectileIdWeaponSlot()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateAppliedFireTickResult());

        string message = log.Entries[1].Message;
        Assert.Contains("record_index=1", message);
        Assert.Contains("tank_id=1", message);
        Assert.Contains("projectile_id=42", message);
        Assert.Contains("weapon_slot=0", message);
    }

    #endregion

    #region Mixed rejected / applied

    [Fact]
    public void CreateTickLog_MixedAppliedAndRejected_EmitsFireEventsInRecordIndexOrder()
    {
        CombinedRuntimeTickResult tickResult = CreateMixedRejectedThenAppliedTickResult();
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        Assert.Equal(3, log.Entries.Count);
        Assert.Equal(CombatLogEventTypes.ScriptTick, log.Entries[0].EventType);
        Assert.Equal(CombatLogEventTypes.FireRejected, log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, log.Entries[2].EventType);
        Assert.Contains("record_index=0", log.Entries[1].Message);
        Assert.Contains("record_index=1", log.Entries[2].Message);
    }

    [Fact]
    public void CreateTickLog_RejectedFire_MessageMatchesExpected()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateMixedRejectedThenAppliedTickResult());

        Assert.Equal(
            ExpectedRejectedMessage(tick: 10, recordIndex: 0, tankId: 0),
            log.Entries[1].Message);
    }

    [Fact]
    public void CreateTickLog_RejectedFire_MessageContainsReasonNotReady()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateMixedRejectedThenAppliedTickResult());

        Assert.Contains("reason=not_ready", log.Entries[1].Message);
    }

    #endregion

    #region Ordering and determinism

    [Fact]
    public void CreateTickLog_PerRecordFireLogs_AreInAscendingRecordIndex()
    {
        CombatLog log = CombinedRuntimeTickCombatLogFactory.CreateTickLog(
            CreateMixedRejectedThenAppliedTickResult());

        Assert.Equal(CombatLogEventTypes.FireRejected, log.Entries[1].EventType);
        Assert.Equal(CombatLogEventTypes.FireRequestApplied, log.Entries[2].EventType);
        Assert.Contains("record_index=0", log.Entries[1].Message);
        Assert.Contains("record_index=1", log.Entries[2].Message);
    }

    [Fact]
    public void CreateTickLog_Deterministic_RepeatedCalls()
    {
        CombinedRuntimeTickResult tickResult = CreateAppliedFireTickResult();

        CombatLog a = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);
        CombatLog b = CombinedRuntimeTickCombatLogFactory.CreateTickLog(tickResult);

        Assert.Equal(a.Entries.Count, b.Entries.Count);
        for (int i = 0; i < a.Entries.Count; i++)
        {
            Assert.Equal(a.Entries[i].Tick, b.Entries[i].Tick);
            Assert.Equal(a.Entries[i].Category, b.Entries[i].Category);
            Assert.Equal(a.Entries[i].EventType, b.Entries[i].EventType);
            Assert.Equal(a.Entries[i].Message, b.Entries[i].Message);
        }
    }

    #endregion
}
