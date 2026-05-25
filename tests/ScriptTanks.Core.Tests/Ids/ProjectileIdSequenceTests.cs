using System;
using ScriptTanks.Core.Ids;

namespace ScriptTanks.Core.Tests.Ids;

public sealed class ProjectileIdSequenceTests
{
    [Fact]
    public void Constructor_StoresNextValue()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(17);

        Assert.Equal(17, seq.NextValue);
    }

    [Fact]
    public void Constructor_AllowsZero()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(0);

        Assert.Equal(0, seq.NextValue);
    }

    [Fact]
    public void Constructor_RejectsNegative_ParamNameNextValue()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ProjectileIdSequence(-1));

        Assert.Equal("nextValue", ex.ParamName);
    }

    [Fact]
    public void Peek_ReturnsCurrentProjectileId()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(42);

        Assert.Equal(new ProjectileId(42), seq.Peek());
    }

    [Fact]
    public void Peek_DoesNotAdvanceSequence()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(5);

        ProjectileId first = seq.Peek();
        ProjectileId second = seq.Peek();

        Assert.Equal(first, second);
        Assert.Equal(5, seq.NextValue);
    }

    [Fact]
    public void AllocateNext_ReturnsCurrentProjectileId()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(9);

        ProjectileIdAllocation alloc = seq.AllocateNext();

        Assert.Equal(new ProjectileId(9), alloc.ProjectileId);
    }

    [Fact]
    public void AllocateNext_ReturnsNextSequenceWithIncrementedNextValue()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(9);

        ProjectileIdAllocation alloc = seq.AllocateNext();

        Assert.Equal(10, alloc.NextSequence.NextValue);
    }

    [Fact]
    public void AllocateNext_DoesNotMutateOriginalSequence()
    {
        ProjectileIdSequence original = new ProjectileIdSequence(3);

        _ = original.AllocateNext();

        Assert.Equal(3, original.NextValue);
        Assert.Equal(new ProjectileId(3), original.Peek());
    }

    [Fact]
    public void MultipleAllocationsFromReturnedSequence_YieldsZeroOneTwo_FromZeroStart()
    {
        ProjectileIdSequence s0 = new ProjectileIdSequence(0);

        ProjectileIdAllocation a0 = s0.AllocateNext();
        ProjectileIdAllocation a1 = a0.NextSequence.AllocateNext();
        ProjectileIdAllocation a2 = a1.NextSequence.AllocateNext();

        Assert.Equal(new ProjectileId(0), a0.ProjectileId);
        Assert.Equal(new ProjectileId(1), a1.ProjectileId);
        Assert.Equal(new ProjectileId(2), a2.ProjectileId);
        Assert.Equal(3, a2.NextSequence.NextValue);
    }

    [Fact]
    public void AllocateNext_WorksWhenStartingFromNonZero()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(100);

        ProjectileIdAllocation alloc = seq.AllocateNext();

        Assert.Equal(new ProjectileId(100), alloc.ProjectileId);
        Assert.Equal(101, alloc.NextSequence.NextValue);
    }

    [Fact]
    public void AllocateNext_AtIntMaxValue_ThrowsInvalidOperationException()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(int.MaxValue);

        Assert.Throws<InvalidOperationException>(() => seq.AllocateNext());
    }

    [Fact]
    public void Equality_True_ForSameNextValue()
    {
        ProjectileIdSequence a = new ProjectileIdSequence(7);
        ProjectileIdSequence b = new ProjectileIdSequence(7);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_False_ForDifferentNextValue()
    {
        ProjectileIdSequence a = new ProjectileIdSequence(7);
        ProjectileIdSequence b = new ProjectileIdSequence(8);

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualValues()
    {
        ProjectileIdSequence a = new ProjectileIdSequence(42);
        ProjectileIdSequence b = new ProjectileIdSequence(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsTypeNameAndNextValue()
    {
        ProjectileIdSequence seq = new ProjectileIdSequence(123);

        string text = seq.ToString();

        Assert.Contains("ProjectileIdSequence", text, StringComparison.Ordinal);
        Assert.Contains("123", text, StringComparison.Ordinal);
    }
}
