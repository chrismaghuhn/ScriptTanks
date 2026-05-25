using System;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogEntryTests
{
    [Fact]
    public void Constructor_PreservesValues()
    {
        CombatLogEntry entry = new CombatLogEntry(
            new SimTick(7),
            CombatLogCategory.Projectile,
            "projectile_spawned",
            "Tank 0 fired StandardCannon.");

        Assert.Equal(new SimTick(7), entry.Tick);
        Assert.Equal(CombatLogCategory.Projectile, entry.Category);
        Assert.Equal("projectile_spawned", entry.EventType);
        Assert.Equal("Tank 0 fired StandardCannon.", entry.Message);
    }

    [Fact]
    public void Constructor_RejectsNullEventType()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                null!,
                "msg"));
        Assert.Equal("eventType", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyEventType()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                string.Empty,
                "msg"));
        Assert.Equal("eventType", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsWhitespaceEventType()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                "   ",
                "msg"));
        Assert.Equal("eventType", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullMessage()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                "event",
                null!));
        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyMessage()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                "event",
                string.Empty));
        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsWhitespaceMessage()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLogEntry(
                SimTick.Zero,
                CombatLogCategory.General,
                "event",
                "\t  \n"));
        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsEveryCategory()
    {
        foreach (CombatLogCategory category in Enum.GetValues<CombatLogCategory>())
        {
            CombatLogEntry entry = new CombatLogEntry(
                SimTick.Zero,
                category,
                "event",
                "message");

            Assert.Equal(category, entry.Category);
        }
    }

    [Fact]
    public void Constructor_DoesNotTrimEventTypeOrMessage()
    {
        CombatLogEntry entry = new CombatLogEntry(
            SimTick.Zero,
            CombatLogCategory.Projectile,
            "  projectile_hit  ",
            "  Hit registered.  ");

        Assert.Equal("  projectile_hit  ", entry.EventType);
        Assert.Equal("  Hit registered.  ", entry.Message);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        CombatLogEntry a = new CombatLogEntry(
            new SimTick(3),
            CombatLogCategory.Damage,
            "tank_destroyed",
            "Tank 1 destroyed.");
        CombatLogEntry b = new CombatLogEntry(
            new SimTick(3),
            CombatLogCategory.Damage,
            "tank_destroyed",
            "Tank 1 destroyed.");

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
