using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogTests
{
    private static CombatLogEntry CreateEntry(
        int tick,
        CombatLogCategory category = CombatLogCategory.General,
        string eventType = "event",
        string message = "message")
        => new CombatLogEntry(new SimTick(tick), category, eventType, message);

    [Fact]
    public void Constructor_AllowsEmptyEntries()
    {
        CombatLog log = new CombatLog(Array.Empty<CombatLogEntry>());

        Assert.Empty(log.Entries);
    }

    [Fact]
    public void Constructor_PreservesEntriesInOrder()
    {
        CombatLogEntry e0 = CreateEntry(0);
        CombatLogEntry e1 = CreateEntry(1);
        CombatLogEntry e2 = CreateEntry(2);

        CombatLog log = new CombatLog(new[] { e0, e1, e2 });

        Assert.Equal(3, log.Entries.Count);
        Assert.Same(e0, log.Entries[0]);
        Assert.Same(e1, log.Entries[1]);
        Assert.Same(e2, log.Entries[2]);
    }

    [Fact]
    public void Constructor_DefensivelyCopiesInputArray()
    {
        CombatLogEntry e0 = CreateEntry(0);
        CombatLogEntry e1 = CreateEntry(1);
        CombatLogEntry[] input = new[] { e0, e1 };

        CombatLog log = new CombatLog(input);

        input[1] = CreateEntry(99);

        Assert.Equal(2, log.Entries.Count);
        Assert.Same(e0, log.Entries[0]);
        Assert.Same(e1, log.Entries[1]);
        Assert.Equal(1, log.Entries[1].Tick.Value);
    }

    [Fact]
    public void Constructor_RejectsNullEntries()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CombatLog(null!));
    }

    [Fact]
    public void Constructor_RejectsNullEntryItem()
    {
        CombatLogEntry[] input = new[] { CreateEntry(0), null! };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLog(input));
        Assert.Equal("entries", ex.ParamName);
    }

    [Fact]
    public void Constructor_AllowsMultipleEntriesOnSameTick()
    {
        CombatLogEntry first = CreateEntry(0, eventType: "first");
        CombatLogEntry second = CreateEntry(0, eventType: "second");
        CombatLogEntry third = CreateEntry(1, eventType: "third");

        CombatLog log = new CombatLog(new[] { first, second, third });

        Assert.Equal(3, log.Entries.Count);
        Assert.Equal("first", log.Entries[0].EventType);
        Assert.Equal("second", log.Entries[1].EventType);
        Assert.Equal("third", log.Entries[2].EventType);
    }

    [Fact]
    public void Constructor_RejectsDecreasingTickOrder()
    {
        CombatLogEntry[] input = new[]
        {
            CreateEntry(0),
            CreateEntry(2),
            CreateEntry(1),
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new CombatLog(input));
        Assert.Equal("entries", ex.ParamName);
    }

    [Fact]
    public void EntriesCollection_IsReadOnly()
    {
        CombatLog log = new CombatLog(new[]
        {
            CreateEntry(0),
            CreateEntry(1),
        });

        IList<CombatLogEntry> list = (IList<CombatLogEntry>)log.Entries;

        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Add(CreateEntry(99)));
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        CombatLogEntry[] entries = new[]
        {
            CreateEntry(0),
            CreateEntry(1),
        };

        CombatLog a = new CombatLog(entries);
        CombatLog b = new CombatLog(entries);

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
