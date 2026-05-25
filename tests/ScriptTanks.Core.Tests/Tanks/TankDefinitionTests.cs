using System;
using System.Collections.Generic;
using ScriptTanks.Core.Math;
using ScriptTanks.Core.Tanks;
using Xunit;

namespace ScriptTanks.Core.Tests.Tanks;

public sealed class TankDefinitionTests
{
    private static BasicTankStats ValidStats()
        => new BasicTankStats(
            maxHitPoints: 100,
            armorReductionPercent: 20,
            hitboxRadius: Fixed.FromInt(2),
            maxVelocityPerTick: Fixed.FromRatio(1, 10),
            bodyTurnRatePerTick: Fixed.FromRatio(1, 20),
            turretTurnRatePerTick: Fixed.FromRatio(1, 10));

    private static List<string> DefaultTags() => new List<string> { "test", "basic" };

    private static TankDefinition DefaultDefinition()
        => new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "A simple test tank.",
            stats: ValidStats(),
            tags: DefaultTags());

    [Fact]
    public void Constructor_PreservesId()
    {
        TankDefinition def = DefaultDefinition();
        Assert.Equal("basic_tank", def.Id);
    }

    [Fact]
    public void Constructor_PreservesDisplayName()
    {
        TankDefinition def = DefaultDefinition();
        Assert.Equal("Basic Tank", def.DisplayName);
    }

    [Fact]
    public void Constructor_PreservesDescription()
    {
        TankDefinition def = DefaultDefinition();
        Assert.Equal("A simple test tank.", def.Description);
    }

    [Fact]
    public void Constructor_PreservesStats()
    {
        BasicTankStats stats = ValidStats();
        TankDefinition def = new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "A simple test tank.",
            stats: stats,
            tags: DefaultTags());

        Assert.Equal(stats, def.Stats);
    }

    [Fact]
    public void Constructor_PreservesTags()
    {
        TankDefinition def = DefaultDefinition();

        Assert.Equal(2, def.Tags.Count);
        Assert.Equal("test", def.Tags[0]);
        Assert.Equal("basic", def.Tags[1]);
    }

    [Fact]
    public void Constructor_PreservesTagOrder()
    {
        TankDefinition def = new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "A simple test tank.",
            stats: ValidStats(),
            tags: new[] { "a", "b", "c" });

        Assert.Equal(3, def.Tags.Count);
        Assert.Equal("a", def.Tags[0]);
        Assert.Equal("b", def.Tags[1]);
        Assert.Equal("c", def.Tags[2]);
    }

    [Fact]
    public void Constructor_AcceptsEmptyDescription()
    {
        TankDefinition def = new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: string.Empty,
            stats: ValidStats(),
            tags: DefaultTags());

        Assert.Equal(string.Empty, def.Description);
    }

    [Fact]
    public void Constructor_AcceptsEmptyTags()
    {
        TankDefinition def = new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "desc",
            stats: ValidStats(),
            tags: Array.Empty<string>());

        Assert.Empty(def.Tags);
    }

    [Fact]
    public void Rejects_NullId()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankDefinition(
                id: null!,
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_EmptyId()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: string.Empty,
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_WhitespaceId()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "   ",
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_NullDisplayName()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: null!,
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_EmptyDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: string.Empty,
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_WhitespaceDisplayName()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "   ",
                description: "desc",
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_NullDescription()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "Basic Tank",
                description: null!,
                stats: ValidStats(),
                tags: DefaultTags()));
    }

    [Fact]
    public void Rejects_NullTags()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: null!));
    }

    [Fact]
    public void Rejects_NullTagInsideCollection()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: new string[] { "ok", null! }));
    }

    [Fact]
    public void Rejects_EmptyTagInsideCollection()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: new string[] { "ok", string.Empty }));
    }

    [Fact]
    public void Rejects_WhitespaceTagInsideCollection()
    {
        Assert.Throws<ArgumentException>(
            () => new TankDefinition(
                id: "basic_tank",
                displayName: "Basic Tank",
                description: "desc",
                stats: ValidStats(),
                tags: new string[] { "ok", "   " }));
    }

    [Fact]
    public void DefensivelyCopies_Tags()
    {
        List<string> tags = new List<string> { "a", "b" };
        TankDefinition def = new TankDefinition(
            id: "basic_tank",
            displayName: "Basic Tank",
            description: "desc",
            stats: ValidStats(),
            tags: tags);

        tags.Add("extra");

        Assert.Equal(2, def.Tags.Count);
        Assert.Equal("a", def.Tags[0]);
        Assert.Equal("b", def.Tags[1]);
    }
}
