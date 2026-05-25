using System;
using ScriptTanks.Core.Arena;
using ScriptTanks.Core.Diagnostics;
using ScriptTanks.Core.Ids;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Match;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Movement;
using ScriptTanks.Core.Projectiles;
using ScriptTanks.Core.Replay;
using ScriptTanks.Core.Simulation;
using ScriptTanks.Core.Tanks;
using ScriptTanks.Core.Weapons;
using Xunit;

namespace ScriptTanks.Core.Tests.Diagnostics;

public sealed class MatchDebugSnapshotTextFormatterTests
{
    private static TankState CreateTank(int id, int ownerSlot)
    {
        return new TankState(
            new TankId(id),
            new PlayerSlot(ownerSlot),
            TankCatalog.BasicTank,
            new MovementState(
                FixedVec2.FromInts(10 + id, 20),
                FixedVec2.Zero),
            TankCatalog.BasicTank.Stats.MaxHitPoints,
            bodyRotation: Fixed.Zero,
            turretRotation: Fixed.Zero);
    }

    private static TankWeaponLoadout CreateLoadout()
        => new TankWeaponLoadout(new[]
        {
            WeaponState.Ready(WeaponCatalog.StandardCannon),
        });

    private static ProjectileState CreateProjectile()
    {
        ProjectileDefinition definition = new ProjectileDefinition(
            rawDamage: 25,
            speedPerTick: Fixed.FromInt(1),
            maxRange: Fixed.FromInt(40),
            radius: Fixed.FromRatio(1, 4));

        return new ProjectileState(
            new ProjectileId(3),
            definition,
            new TankId(0),
            new WeaponSlot(0),
            new SimTick(5),
            FixedVec2.FromInts(10, 20),
            FixedVec2.FromInts(1, 0),
            definition.MaxRange,
            isActive: true);
    }

    private static MatchState CreateState(params ProjectileState[] projectiles)
    {
        return new MatchState(
            ArenaCatalog.OpenTestArena,
            new SimTick(7),
            new[]
            {
                CreateTank(0, 0),
                CreateTank(1, 1),
            },
            new[]
            {
                CreateLoadout(),
                CreateLoadout(),
            },
            projectiles);
    }

    private static CombatLog CreateLog()
    {
        return new CombatLogBuilder()
            .Add(
                new SimTick(7),
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchStarted,
                "Match started.")
            .Build();
    }

    [Fact]
    public void FormatLines_IncludesHeaderAndStateSummary()
    {
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(CreateState());

        IReadOnlyList<string> lines = MatchDebugSnapshotTextFormatter.FormatLines(snapshot);

        Assert.Contains("Debug Snapshot", lines);
        Assert.Contains("Tick: 7", lines);
        Assert.Contains($"Arena: {ArenaCatalog.OpenTestArena.Id}", lines);
        Assert.Contains("ReplayFrame: none", lines);
        Assert.Contains("Tanks: 2", lines);
        Assert.Contains("Projectiles: 0", lines);
        Assert.Contains("LogEntries: 0", lines);
    }

    [Fact]
    public void FormatLines_IncludesProjectileCount()
    {
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(
            CreateState(CreateProjectile()));

        IReadOnlyList<string> lines = MatchDebugSnapshotTextFormatter.FormatLines(snapshot);

        Assert.Contains("Projectiles: 1", lines);
    }

    [Fact]
    public void FormatLines_IncludesReplayFrameIndex()
    {
        MatchState state = CreateState();
        MatchReplayFrame frame = new MatchReplayFrame(4, state);
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(state, replayFrame: frame);

        IReadOnlyList<string> lines = MatchDebugSnapshotTextFormatter.FormatLines(snapshot);

        Assert.Contains("ReplayFrame: 4", lines);
    }

    [Fact]
    public void FormatLines_IndentsCombatLogLines()
    {
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(
            CreateState(),
            CreateLog());

        IReadOnlyList<string> lines = MatchDebugSnapshotTextFormatter.FormatLines(snapshot);

        Assert.Contains("Log:", lines);
        Assert.Contains("LogEntries: 1", lines);
        Assert.Contains(lines, line => line.StartsWith("  ", StringComparison.Ordinal) && line.Contains("Match started."));
    }

    [Fact]
    public void Format_JoinsLinesWithEnvironmentNewLine()
    {
        MatchDebugSnapshot snapshot = new MatchDebugSnapshot(CreateState());

        string formatted = MatchDebugSnapshotTextFormatter.Format(snapshot);

        Assert.Equal(
            string.Join(Environment.NewLine, MatchDebugSnapshotTextFormatter.FormatLines(snapshot)),
            formatted);
    }
}
