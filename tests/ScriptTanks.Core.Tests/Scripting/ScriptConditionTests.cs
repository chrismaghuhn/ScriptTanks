using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptConditionTests
{
    [Fact]
    public void Constructor_PreservesType()
    {
        var condition = new ScriptCondition(ScriptConditionType.WeaponReady, "x");

        Assert.Equal(ScriptConditionType.WeaponReady, condition.Type);
    }

    [Fact]
    public void Constructor_PreservesArgument()
    {
        var condition = new ScriptCondition(ScriptConditionType.EnemyVisible, "tank1");

        Assert.Equal("tank1", condition.Argument);
    }

    [Fact]
    public void Constructor_PreservesArgumentVerbatimWithoutTrimming()
    {
        var condition = new ScriptCondition(
            ScriptConditionType.MyHpBelow,
            "  30  ");

        Assert.Equal("  30  ", condition.Argument);
    }

    [Fact]
    public void Constructor_AllowsEmptyArgument()
    {
        var condition = new ScriptCondition(ScriptConditionType.SensorReady, string.Empty);

        Assert.Equal(string.Empty, condition.Argument);
    }

    [Fact]
    public void Constructor_AllowsWhitespaceArgument()
    {
        var condition = new ScriptCondition(ScriptConditionType.Always, "   ");

        Assert.Equal("   ", condition.Argument);
    }

    [Fact]
    public void Constructor_RejectsNullArgument_WithParamNameArgument()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ScriptCondition(ScriptConditionType.EnemyVisible, null!));

        Assert.Equal("argument", ex.ParamName);
    }

    [Fact]
    public void Constructor_RejectsUndefinedConditionType_WithParamNameType()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScriptCondition(
                (ScriptConditionType)999,
                string.Empty));

        Assert.Equal("type", ex.ParamName);
    }

    [Fact]
    public void Always_ReturnsAlwaysType()
    {
        ScriptCondition condition = ScriptCondition.Always();

        Assert.Equal(ScriptConditionType.Always, condition.Type);
    }

    [Fact]
    public void Always_ReturnsEmptyArgument()
    {
        ScriptCondition condition = ScriptCondition.Always();

        Assert.Equal(string.Empty, condition.Argument);
    }

    [Fact]
    public void Equals_ReturnsTrue_ForSameTypeAndSameArgument()
    {
        var a = new ScriptCondition(ScriptConditionType.MyHpBelow, "30");
        var b = new ScriptCondition(ScriptConditionType.MyHpBelow, "30");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentType()
    {
        var a = new ScriptCondition(ScriptConditionType.MyHpBelow, "30");
        var b = new ScriptCondition(ScriptConditionType.WeaponReady, "30");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_ReturnsFalse_ForDifferentArgument()
    {
        var a = new ScriptCondition(ScriptConditionType.MyHpBelow, "20");
        var b = new ScriptCondition(ScriptConditionType.MyHpBelow, "40");

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Operators_MatchEqualsSemantics()
    {
        var x = new ScriptCondition(ScriptConditionType.EnemyVisible, "a");
        var y = new ScriptCondition(ScriptConditionType.EnemyVisible, "a");
        var z = new ScriptCondition(ScriptConditionType.WeaponReady, "a");

        Assert.True(x == y);
        Assert.False(x == z);
        Assert.False(x != y);
        Assert.True(x != z);
    }

    [Fact]
    public void GetHashCode_IsConsistentForEqualValues()
    {
        var a = new ScriptCondition(ScriptConditionType.EnemyDistanceBelow, "20");
        var b = new ScriptCondition(ScriptConditionType.EnemyDistanceBelow, "20");

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_IncludesTypeNameAndArgumentUsingInvariantCulture()
    {
        var condition = new ScriptCondition(
            ScriptConditionType.EnemyDistanceBelow,
            "20");

        string value = condition.ToString();

        Assert.Contains("ScriptCondition", value, StringComparison.Ordinal);
        Assert.Contains("Type = EnemyDistanceBelow", value, StringComparison.Ordinal);
        Assert.Contains("Argument = 20", value, StringComparison.Ordinal);
    }
}
