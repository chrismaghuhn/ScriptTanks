using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptRoutineTests
{
    private static ScriptCondition CreateCondition()
    {
        return new ScriptCondition(
            ScriptConditionType.EnemyVisible,
            string.Empty);
    }

    private static ScriptCommand CreateCommand()
    {
        return new ScriptCommand(
            ScriptCommandType.Fire,
            string.Empty);
    }

    [Fact]
    public void Constructor_PreservesName()
    {
        var routine = new ScriptRoutine(
            "engage",
            CreateCondition(),
            CreateCommand());

        Assert.Equal("engage", routine.Name);
    }

    [Fact]
    public void Constructor_PreservesCondition()
    {
        ScriptCondition condition = CreateCondition();

        var routine = new ScriptRoutine("r", condition, CreateCommand());

        Assert.Equal(condition, routine.Condition);
    }

    [Fact]
    public void Constructor_PreservesCommand()
    {
        ScriptCommand command = CreateCommand();

        var routine = new ScriptRoutine("r", CreateCondition(), command);

        Assert.Equal(command, routine.Command);
    }

    [Fact]
    public void Constructor_PreservesNameVerbatimWithoutTrimming()
    {
        var routine = new ScriptRoutine(
            "  engage  ",
            CreateCondition(),
            CreateCommand());

        Assert.Equal("  engage  ", routine.Name);
    }

    [Fact]
    public void Constructor_RejectsNullName_WithParamNameName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ScriptRoutine(
                null!,
                CreateCondition(),
                CreateCommand()));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsEmptyName_WithParamNameName()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new ScriptRoutine(
                string.Empty,
                CreateCondition(),
                CreateCommand()));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsWhitespaceName_WithParamNameName()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new ScriptRoutine(
                "   ",
                CreateCondition(),
                CreateCommand()));

        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameNameConditionAndCommand()
    {
        var a = new ScriptRoutine("engage", CreateCondition(), CreateCommand());
        var b = new ScriptRoutine("engage", CreateCondition(), CreateCommand());

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentName()
    {
        var a = new ScriptRoutine("a", CreateCondition(), CreateCommand());
        var b = new ScriptRoutine("b", CreateCondition(), CreateCommand());

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentCondition()
    {
        var a = new ScriptRoutine(
            "r",
            new ScriptCondition(ScriptConditionType.WeaponReady, "x"),
            CreateCommand());
        var b = new ScriptRoutine(
            "r",
            new ScriptCondition(ScriptConditionType.EnemyVisible, "x"),
            CreateCommand());

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentCommand()
    {
        var a = new ScriptRoutine(
            "r",
            CreateCondition(),
            new ScriptCommand(ScriptCommandType.Fire, "a"));
        var b = new ScriptRoutine(
            "r",
            CreateCondition(),
            new ScriptCommand(ScriptCommandType.Fire, "b"));

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Operators_MatchEqualsSemantics()
    {
        var x = new ScriptRoutine("k", CreateCondition(), CreateCommand());
        var y = new ScriptRoutine("k", CreateCondition(), CreateCommand());
        var z = new ScriptRoutine("j", CreateCondition(), CreateCommand());

        Assert.True(x == y);
        Assert.False(x == z);
        Assert.False(x != y);
        Assert.True(x != z);
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        var a = new ScriptRoutine("engage", CreateCondition(), CreateCommand());
        var b = new ScriptRoutine("engage", CreateCondition(), CreateCommand());

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_IncludesRoutineNameConditionAndCommand()
    {
        var routine = new ScriptRoutine(
            "engage",
            CreateCondition(),
            CreateCommand());

        string value = routine.ToString();

        Assert.Contains("ScriptRoutine", value, StringComparison.Ordinal);
        Assert.Contains("Name = engage", value, StringComparison.Ordinal);
        Assert.Contains("Condition =", value, StringComparison.Ordinal);
        Assert.Contains("Command =", value, StringComparison.Ordinal);
    }
}
