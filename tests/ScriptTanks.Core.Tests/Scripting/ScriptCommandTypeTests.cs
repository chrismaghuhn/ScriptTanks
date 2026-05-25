using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptCommandTypeTests
{
    [Fact]
    public void EnumValues_AreExplicitAndStable()
    {
        Assert.Equal(0, (int)ScriptCommandType.NoOp);
        Assert.Equal(1, (int)ScriptCommandType.ScanEnemy);
        Assert.Equal(2, (int)ScriptCommandType.AimAtEnemy);
        Assert.Equal(3, (int)ScriptCommandType.Fire);
        Assert.Equal(4, (int)ScriptCommandType.MoveToPatrolPoint);
        Assert.Equal(5, (int)ScriptCommandType.Retreat);
    }

    [Fact]
    public void Enum_ContainsAllMvpCommandTypes()
    {
        ScriptCommandType[] values =
            Enum.GetValues<ScriptCommandType>();

        Assert.Equal(6, values.Length);
        Assert.Contains(ScriptCommandType.NoOp, values);
        Assert.Contains(ScriptCommandType.ScanEnemy, values);
        Assert.Contains(ScriptCommandType.AimAtEnemy, values);
        Assert.Contains(ScriptCommandType.Fire, values);
        Assert.Contains(ScriptCommandType.MoveToPatrolPoint, values);
        Assert.Contains(ScriptCommandType.Retreat, values);
    }
}
