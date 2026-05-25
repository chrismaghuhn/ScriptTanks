using System;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptTranslatedCommandRequestKindTests
{
    [Fact]
    public void Enum_values_are_explicit_and_stable()
    {
        Assert.Equal(0, (int)ScriptTranslatedCommandRequestKind.None);
        Assert.Equal(1, (int)ScriptTranslatedCommandRequestKind.NoOp);
        Assert.Equal(2, (int)ScriptTranslatedCommandRequestKind.ScanEnemy);
        Assert.Equal(3, (int)ScriptTranslatedCommandRequestKind.AimAtEnemy);
        Assert.Equal(4, (int)ScriptTranslatedCommandRequestKind.Fire);
        Assert.Equal(5, (int)ScriptTranslatedCommandRequestKind.MoveToPatrolPoint);
        Assert.Equal(6, (int)ScriptTranslatedCommandRequestKind.Retreat);
    }

    [Fact]
    public void Enum_contains_all_expected_values()
    {
        string[] names = Enum.GetNames<ScriptTranslatedCommandRequestKind>();

        Assert.Equal(7, names.Length);
    }
}
