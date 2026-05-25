using System;
using System.Linq;
using ScriptTanks.Core.Scripting;
using Xunit;

namespace ScriptTanks.Core.Tests.Scripting;

public sealed class ScriptMappedTurretRequestApplicationStatusTests
{
    [Theory]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret, 0)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.Applied, 1)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest, 2)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange, 3)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.TankDestroyed, 4)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.NoTarget, 5)]
    [InlineData(ScriptMappedTurretRequestApplicationStatus.MissingAimSolution, 6)]
    public void Enum_members_have_explicit_stable_values(
        ScriptMappedTurretRequestApplicationStatus status,
        int expected)
    {
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public void Enum_contains_exactly_expected_members()
    {
        string[] names = Enum.GetNames<ScriptMappedTurretRequestApplicationStatus>();

        Assert.Equal(7, names.Length);

        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.SkippedNotTurret), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.Applied), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.RejectedInvalidTurretRequest), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.TankIndexOutOfRange), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.TankDestroyed), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.NoTarget), names);
        Assert.Contains(nameof(ScriptMappedTurretRequestApplicationStatus.MissingAimSolution), names);

        Assert.Equal(
            7,
            Enum.GetValues<ScriptMappedTurretRequestApplicationStatus>().Cast<int>().Distinct().Count());
    }
}
