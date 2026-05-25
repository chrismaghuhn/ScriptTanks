using System;
using System.Collections.Generic;
using ScriptTanks.Core.Logging;
using ScriptTanks.Core.Simulation;
using Xunit;

namespace ScriptTanks.Core.Tests.Logging;

public sealed class CombatLogMergerTests
{
    private static CombatLogEntry CreateEntry(
        int tick,
        string eventType = "event",
        string message = "message")
    {
        return new CombatLogEntry(
            new SimTick(tick),
            CombatLogCategory.General,
            eventType,
            message);
    }

    private static CombatLog CreateLog(params CombatLogEntry[] entries)
    {
        return new CombatLog(entries);
    }

    [Fact]
    public void MergeParams_AllowsNoLogs_ReturnsEmptyLog()
    {
        CombatLog merged = CombatLogMerger.Merge();

        Assert.Empty(merged.Entries);
    }

    [Fact]
    public void MergeEnumerable_RejectsNullLogsSequence()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => CombatLogMerger.Merge((IEnumerable<CombatLog>)null!));
        Assert.Equal("logs", ex.ParamName);
    }

    [Fact]
    public void MergeParams_RejectsNullLogItem()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CombatLogMerger.Merge(CreateLog(), null!));
        Assert.Equal("logs", ex.ParamName);
    }

    [Fact]
    public void MergeEnumerable_RejectsNullLogItem()
    {
        CombatLog?[] logs = new CombatLog?[]
        {
            CreateLog(),
            null,
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CombatLogMerger.Merge(logs!));
        Assert.Equal("logs", ex.ParamName);
    }

    [Fact]
    public void Merge_SingleLog_PreservesEntries()
    {
        CombatLogEntry entry0 = CreateEntry(0, "a");
        CombatLogEntry entry1 = CreateEntry(1, "b");
        CombatLog log = CreateLog(entry0, entry1);

        CombatLog merged = CombatLogMerger.Merge(log);

        Assert.Equal(2, merged.Entries.Count);
        Assert.Same(entry0, merged.Entries[0]);
        Assert.Same(entry1, merged.Entries[1]);
    }

    [Fact]
    public void Merge_MultipleLogs_PreservesInputLogOrder()
    {
        CombatLog logA = CreateLog(CreateEntry(0, "a"));
        CombatLog logB = CreateLog(CreateEntry(1, "b"));
        CombatLog logC = CreateLog(CreateEntry(2, "c"));

        CombatLog merged = CombatLogMerger.Merge(logA, logB, logC);

        Assert.Equal(3, merged.Entries.Count);
        Assert.Equal("a", merged.Entries[0].EventType);
        Assert.Equal("b", merged.Entries[1].EventType);
        Assert.Equal("c", merged.Entries[2].EventType);
    }

    [Fact]
    public void Merge_AllowsSameTickAcrossLogs()
    {
        CombatLog logA = CreateLog(CreateEntry(0, "first"));
        CombatLog logB = CreateLog(CreateEntry(0, "second"));
        CombatLog logC = CreateLog(CreateEntry(1, "third"));

        CombatLog merged = CombatLogMerger.Merge(logA, logB, logC);

        Assert.Equal(3, merged.Entries.Count);
        Assert.Equal("first", merged.Entries[0].EventType);
        Assert.Equal("second", merged.Entries[1].EventType);
        Assert.Equal("third", merged.Entries[2].EventType);
    }

    [Fact]
    public void Merge_RejectsDecreasingTickAcrossLogs()
    {
        CombatLog logA = CreateLog(CreateEntry(2, "a"));
        CombatLog logB = CreateLog(CreateEntry(1, "b"));

        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => CombatLogMerger.Merge(logA, logB));
        Assert.Equal("entries", ex.ParamName);
    }

    [Fact]
    public void Merge_ReturnsImmutableSnapshotUnaffectedBySourceArrayMutation()
    {
        CombatLog log0 = CreateLog(CreateEntry(0, "a"));
        CombatLog log1 = CreateLog(CreateEntry(1, "b"));
        CombatLog[] source = new[] { log0, log1 };

        CombatLog merged = CombatLogMerger.Merge(source);

        source[1] = CreateLog(CreateEntry(99, "changed"));

        Assert.Equal(2, merged.Entries.Count);
        Assert.Equal("b", merged.Entries[1].EventType);
    }

    [Fact]
    public void Merge_DoesNotMutateSourceLogs()
    {
        CombatLogEntry entry0 = CreateEntry(0, "a");
        CombatLogEntry entry1 = CreateEntry(1, "b");
        CombatLog log0 = CreateLog(entry0);
        CombatLog log1 = CreateLog(entry1);

        CombatLogMerger.Merge(log0, log1);

        Assert.Single(log0.Entries);
        Assert.Same(entry0, log0.Entries[0]);
        Assert.Single(log1.Entries);
        Assert.Same(entry1, log1.Entries[0]);
    }
}
