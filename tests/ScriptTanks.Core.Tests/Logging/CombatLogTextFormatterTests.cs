using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogTextFormatterTests
{
    private static CombatLogEntry CreateEntry(
        int tick,
        CombatLogCategory category = CombatLogCategory.General,
        string eventType = "event",
        string message = "message")
    {
        return new CombatLogEntry(
            new SimTick(tick),
            category,
            eventType,
            message);
    }

    private static CombatLog CreateLog(params CombatLogEntry[] entries)
    {
        return new CombatLog(entries);
    }

    [Fact]
    public void FormatLines_RejectsNullLog()
    {
        Assert.Throws<ArgumentNullException>(
            () => CombatLogTextFormatter.FormatLines(null!));
    }

    [Fact]
    public void Format_RejectsNullLog()
    {
        Assert.Throws<ArgumentNullException>(
            () => CombatLogTextFormatter.Format(null!));
    }

    [Fact]
    public void FormatLines_AllowsEmptyLog()
    {
        CombatLog log = CreateLog();

        IReadOnlyList<string> lines = CombatLogTextFormatter.FormatLines(log);

        Assert.Empty(lines);
    }

    [Fact]
    public void Format_AllowsEmptyLog()
    {
        CombatLog log = CreateLog();

        string text = CombatLogTextFormatter.Format(log);

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void FormatLines_FormatsSingleEntry()
    {
        CombatLog log = CreateLog(
            CreateEntry(
                5,
                CombatLogCategory.Weapon,
                CombatLogEventTypes.FireRequested,
                "Tank 0 requested fire from weapon slot 0."));

        IReadOnlyList<string> lines = CombatLogTextFormatter.FormatLines(log);

        string line = Assert.Single(lines);
        Assert.Equal(
            "[0005] Weapon/fire_requested: Tank 0 requested fire from weapon slot 0.",
            line);
    }

    [Fact]
    public void FormatLines_FormatsMultipleEntriesInOrder()
    {
        CombatLog log = CreateLog(
            CreateEntry(
                0,
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchStarted,
                "Match started."),
            CreateEntry(
                5,
                CombatLogCategory.Projectile,
                CombatLogEventTypes.ProjectileSpawned,
                "Projectile 123 spawned from tank 0."),
            CreateEntry(
                6,
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchEnded,
                "Match ended."));

        IReadOnlyList<string> lines = CombatLogTextFormatter.FormatLines(log);

        Assert.Equal(3, lines.Count);
        Assert.Equal("[0000] Match/match_started: Match started.", lines[0]);
        Assert.Equal(
            "[0005] Projectile/projectile_spawned: Projectile 123 spawned from tank 0.",
            lines[1]);
        Assert.Equal("[0006] Match/match_ended: Match ended.", lines[2]);
    }

    [Fact]
    public void FormatLines_PadsTickToFourDigits()
    {
        CombatLog log = CreateLog(
            CreateEntry(0, message: "zero."),
            CreateEntry(42, message: "forty-two."),
            CreateEntry(12345, message: "large."));

        IReadOnlyList<string> lines = CombatLogTextFormatter.FormatLines(log);

        Assert.Equal(3, lines.Count);
        Assert.Equal("[0000] General/event: zero.", lines[0]);
        Assert.Equal("[0042] General/event: forty-two.", lines[1]);
        Assert.Equal("[12345] General/event: large.", lines[2]);
    }

    [Fact]
    public void Format_JoinsLinesWithEnvironmentNewLine()
    {
        CombatLog log = CreateLog(
            CreateEntry(
                0,
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchStarted,
                "Match started."),
            CreateEntry(
                1,
                CombatLogCategory.Match,
                CombatLogEventTypes.MatchEnded,
                "Match ended."));

        string text = CombatLogTextFormatter.Format(log);

        string expected = string.Join(
            Environment.NewLine,
            "[0000] Match/match_started: Match started.",
            "[0001] Match/match_ended: Match ended.");

        Assert.Equal(expected, text);
    }

    [Fact]
    public void FormatLines_ReturnsReadOnlySnapshot()
    {
        CombatLog log = CreateLog(CreateEntry(0));

        IReadOnlyList<string> lines = CombatLogTextFormatter.FormatLines(log);

        IList<string> list = (IList<string>)lines;
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add("extra"));
    }
}
