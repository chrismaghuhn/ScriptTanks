using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Tests.Ids;

public sealed class ProjectileIdAllocationTests
{
    [Fact]
    public void Constructor_StoresProjectileId()
    {
        ProjectileId id = new ProjectileId(5);
        ProjectileIdSequence next = new ProjectileIdSequence(6);

        ProjectileIdAllocation alloc = new ProjectileIdAllocation(id, next);

        Assert.Equal(id, alloc.ProjectileId);
    }

    [Fact]
    public void Constructor_StoresNextSequence()
    {
        ProjectileId id = new ProjectileId(5);
        ProjectileIdSequence next = new ProjectileIdSequence(6);

        ProjectileIdAllocation alloc = new ProjectileIdAllocation(id, next);

        Assert.Equal(next, alloc.NextSequence);
    }

    [Fact]
    public void Equality_True_ForSameValues()
    {
        ProjectileIdAllocation a = new ProjectileIdAllocation(
            new ProjectileId(3),
            new ProjectileIdSequence(4));
        ProjectileIdAllocation b = new ProjectileIdAllocation(
            new ProjectileId(3),
            new ProjectileIdSequence(4));

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_False_ForDifferentProjectileId()
    {
        ProjectileIdAllocation a = new ProjectileIdAllocation(
            new ProjectileId(3),
            new ProjectileIdSequence(4));
        ProjectileIdAllocation b = new ProjectileIdAllocation(
            new ProjectileId(99),
            new ProjectileIdSequence(4));

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equality_False_ForDifferentNextSequence()
    {
        ProjectileIdAllocation a = new ProjectileIdAllocation(
            new ProjectileId(3),
            new ProjectileIdSequence(4));
        ProjectileIdAllocation b = new ProjectileIdAllocation(
            new ProjectileId(3),
            new ProjectileIdSequence(5));

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        ProjectileIdAllocation a = new ProjectileIdAllocation(
            new ProjectileId(1),
            new ProjectileIdSequence(2));
        ProjectileIdAllocation b = new ProjectileIdAllocation(
            new ProjectileId(1),
            new ProjectileIdSequence(2));

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsProjectileIdAndNextSequence()
    {
        ProjectileIdAllocation alloc = new ProjectileIdAllocation(
            new ProjectileId(7),
            new ProjectileIdSequence(8));

        string text = alloc.ToString();

        Assert.Contains("ProjectileIdAllocation", text, StringComparison.Ordinal);
        Assert.Contains("7", text, StringComparison.Ordinal);
        Assert.Contains("8", text, StringComparison.Ordinal);
    }
}
