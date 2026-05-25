using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptConditionTypeTests
{
    [Fact]
    public void EnumValues_AreExplicitAndStable()
    {
        Assert.Equal(0, (int)ScriptConditionType.Always);
        Assert.Equal(1, (int)ScriptConditionType.EnemyVisible);
        Assert.Equal(2, (int)ScriptConditionType.WeaponReady);
        Assert.Equal(3, (int)ScriptConditionType.MyHpBelow);
        Assert.Equal(4, (int)ScriptConditionType.EnemyDistanceBelow);
        Assert.Equal(5, (int)ScriptConditionType.SensorReady);
        Assert.Equal(6, (int)ScriptConditionType.TurretAligned);
        Assert.Equal(7, (int)ScriptConditionType.TurretTurning);
        Assert.Equal(8, (int)ScriptConditionType.HasAimTarget);
        Assert.Equal(9, (int)ScriptConditionType.MissingAimSolution);
    }

    [Fact]
    public void ScriptConditionType_TurretAimStatusValues_AreAppended()
    {
        ScriptConditionType[] values =
            Enum.GetValues<ScriptConditionType>();

        Assert.Equal(10, values.Length);
        Assert.Contains(ScriptConditionType.Always, values);
        Assert.Contains(ScriptConditionType.EnemyVisible, values);
        Assert.Contains(ScriptConditionType.WeaponReady, values);
        Assert.Contains(ScriptConditionType.MyHpBelow, values);
        Assert.Contains(ScriptConditionType.EnemyDistanceBelow, values);
        Assert.Contains(ScriptConditionType.SensorReady, values);
        Assert.Contains(ScriptConditionType.TurretAligned, values);
        Assert.Contains(ScriptConditionType.TurretTurning, values);
        Assert.Contains(ScriptConditionType.HasAimTarget, values);
        Assert.Contains(ScriptConditionType.MissingAimSolution, values);
    }
}
