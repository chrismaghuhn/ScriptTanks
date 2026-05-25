using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTests
{
    [Fact]
    public void Constructor_PreservesType()
    {
        var command = new ScriptCommand(ScriptCommandType.Fire, "x");

        Assert.Equal(ScriptCommandType.Fire, command.Type);
    }

    [Fact]
    public void Constructor_PreservesArgument()
    {
        var command = new ScriptCommand(ScriptCommandType.ScanEnemy, "nearest");

        Assert.Equal("nearest", command.Argument);
    }

    [Fact]
    public void Constructor_PreservesArgumentVerbatimWithoutTrimming()
    {
        var command = new ScriptCommand(
            ScriptCommandType.Fire,
            "  primary  ");

        Assert.Equal("  primary  ", command.Argument);
    }

    [Fact]
    public void Constructor_AllowsEmptyArgument()
    {
        var command = new ScriptCommand(ScriptCommandType.Retreat, string.Empty);

        Assert.Equal(string.Empty, command.Argument);
    }

    [Fact]
    public void Constructor_AllowsWhitespaceArgument()
    {
        var command = new ScriptCommand(ScriptCommandType.NoOp, "   ");

        Assert.Equal("   ", command.Argument);
    }

    [Fact]
    public void Constructor_RejectsNullArgument_WithParamNameArgument()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ScriptCommand(ScriptCommandType.Fire, null!));

        Assert.Equal("argument", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsUndefinedCommandType_WithParamNameType()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScriptCommand(
                (ScriptCommandType)999,
                string.Empty));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void NoOp_ReturnsNoOpType()
    {
        ScriptCommand command = ScriptCommand.NoOp();

        Assert.Equal(ScriptCommandType.NoOp, command.Type);
    }

    [Fact]
    public void NoOp_ReturnsEmptyArgument()
    {
        ScriptCommand command = ScriptCommand.NoOp();

        Assert.Equal(string.Empty, command.Argument);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameTypeAndSameArgument()
    {
        var a = new ScriptCommand(ScriptCommandType.Fire, "primary");
        var b = new ScriptCommand(ScriptCommandType.Fire, "primary");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentType()
    {
        var a = new ScriptCommand(ScriptCommandType.Fire, "x");
        var b = new ScriptCommand(ScriptCommandType.Retreat, "x");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentArgument()
    {
        var a = new ScriptCommand(ScriptCommandType.Fire, "a");
        var b = new ScriptCommand(ScriptCommandType.Fire, "b");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Operators_MatchEqualsSemantics()
    {
        var x = new ScriptCommand(ScriptCommandType.ScanEnemy, "n");
        var y = new ScriptCommand(ScriptCommandType.ScanEnemy, "n");
        var z = new ScriptCommand(ScriptCommandType.Fire, "n");

        Assert.True(x == y);
        Assert.False(x == z);
        Assert.False(x != y);
        Assert.True(x != z);
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        var a = new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next");
        var b = new ScriptCommand(ScriptCommandType.MoveToPatrolPoint, "next");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_IncludesTypeNameAndArgumentUsingInvariantCulture()
    {
        var command = new ScriptCommand(
            ScriptCommandType.ScanEnemy,
            "nearest");

        string value = command.ToString();

        Assert.Contains("ScriptCommand", value, StringComparison.Ordinal);
        Assert.Contains("Type = ScanEnemy", value, StringComparison.Ordinal);
        Assert.Contains("Argument = nearest", value, StringComparison.Ordinal);
    }
}
