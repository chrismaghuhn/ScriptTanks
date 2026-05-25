using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogBuilderTests
{
    private static CombatLogEntry CreateEntry(
        int tick,
        CombatLogCategory category = CombatLogCategory.General,
        string eventType = "event",
        string message = "message")
        => new CombatLogEntry(new SimTick(tick), category, eventType, message);

    [Fact]
    public void NewBuilder_HasCountZero()
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        Assert.Equal(0, builder.Count);
    }

    [Fact]
    public void AddEntry_AppendsEntryAndIncrementsCount()
    {
        CombatLogBuilder builder = new CombatLogBuilder();
        CombatLogEntry entry = CreateEntry(0);

        CombatLogBuilder returned = builder.Add(entry);

        Assert.Same(builder, returned);
        Assert.Equal(1, builder.Count);
        Assert.Same(entry, builder.Build().Entries[0]);
    }

    [Fact]
    public void AddFields_CreatesAndAppendsEntry()
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder.Add(
            new SimTick(3),
            CombatLogCategory.Damage,
            "tank_destroyed",
            "Tank 1 destroyed.");

        CombatLog log = builder.Build();

        Assert.Single(log.Entries);
        Assert.Equal(new SimTick(3), log.Entries[0].Tick);
        Assert.Equal(CombatLogCategory.Damage, log.Entries[0].Category);
        Assert.Equal("tank_destroyed", log.Entries[0].EventType);
        Assert.Equal("Tank 1 destroyed.", log.Entries[0].Message);
    }

    [Fact]
    public void Add_AllowsSameTickEntries()
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        builder
            .Add(CreateEntry(0, eventType: "first"))
            .Add(CreateEntry(0, eventType: "second"))
            .Add(CreateEntry(1, eventType: "third"));

        Assert.Equal(3, builder.Count);

        CombatLog log = builder.Build();
        Assert.Equal("first", log.Entries[0].EventType);
        Assert.Equal("second", log.Entries[1].EventType);
        Assert.Equal("third", log.Entries[2].EventType);
    }

    [Fact]
    public void Add_RejectsNullEntry()
    {
        CombatLogBuilder builder = new CombatLogBuilder();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => builder.Add((CombatLogEntry)null!));
        Assert.Equal("entry", ex.ParamName);
    }

    [Fact]
    public void Add_RejectsDecreasingTickOrder()
    {
        CombatLogBuilder builder = new CombatLogBuilder();
        builder.Add(CreateEntry(0)).Add(CreateEntry(2));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => builder.Add(CreateEntry(1)));

        Assert.Equal("tick", ex.ParamName);
        Assert.Equal(2, builder.Count);
    }

    [Fact]
    public void Build_AllowsEmptyLog()
    {
        CombatLog log = new CombatLogBuilder().Build();

        Assert.Empty(log.Entries);
    }

    [Fact]
    public void Build_ReturnsImmutableCombatLogSnapshot()
    {
        CombatLogBuilder builder = new CombatLogBuilder();
        CombatLogEntry e0 = CreateEntry(0);
        CombatLogEntry e1 = CreateEntry(1);
        builder.Add(e0).Add(e1);

        CombatLog log = builder.Build();

        IList<CombatLogEntry> list = (IList<CombatLogEntry>)log.Entries;
        Assert.True(list.IsReadOnly);
        Assert.Equal(2, log.Entries.Count);
        Assert.Same(e0, log.Entries[0]);
        Assert.Same(e1, log.Entries[1]);
    }

    [Fact]
    public void Build_DoesNotMutateAfterLaterAdds()
    {
        CombatLogBuilder builder = new CombatLogBuilder();
        builder.Add(CreateEntry(0));

        CombatLog first = builder.Build();

        builder.Add(CreateEntry(1));

        CombatLog second = builder.Build();

        Assert.Single(first.Entries);
        Assert.Equal(2, second.Entries.Count);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Equals_UsesReferenceEquality()
    {
        CombatLogBuilder a = new CombatLogBuilder();
        CombatLogBuilder b = new CombatLogBuilder();

        Assert.False(ReferenceEquals(a, b));
        Assert.False(a.Equals(b));
        Assert.True(a.Equals(a));
    }
}
